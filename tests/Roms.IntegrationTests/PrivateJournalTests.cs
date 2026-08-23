using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Roms.Application;
using Roms.Domain;
using Roms.Infrastructure.Identity;
using Roms.Infrastructure.Persistence;
using Roms.Infrastructure.Services;

namespace Roms.IntegrationTests;

public sealed class PrivateJournalTests
{
    private static readonly DateTime Now = new(2026, 8, 23, 2, 45, 0, DateTimeKind.Utc);

    [Fact]
    public void Browser_module_has_no_durable_client_storage_or_telemetry_surface()
    {
        var source = File.ReadAllText(FindJournalModule());
        var forbidden = new[]
        {
            "localStorage", "sessionStorage", "indexedDB", "document.cookie",
            "caches.open", "navigator.sendBeacon", "WebSocket(", "EventSource("
        };
        Assert.All(forbidden, value => Assert.DoesNotContain(value, source, StringComparison.Ordinal));
        Assert.Contains("state.plaintext.clear()", source, StringComparison.Ordinal);
        Assert.Contains("state.host.replaceChildren()", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Owner_can_complete_encrypted_entry_lifecycle_without_server_plaintext()
    {
        var fixture = await Fixture.CreateAsync();
        var owner = Principal("waiter-id", "waiter");
        await fixture.Service.SaveKeyEnvelopeAsync(owner, KeyEnvelope());

        const string plaintextSentinel = "PRIVATE JOURNAL SENTINEL SHOULD NEVER REACH SERVER";
        var encryptedPayload = Bytes(80, 41);
        var entryId = await fixture.Service.CreateAsync(owner, Entry(encryptedPayload));

        var active = Assert.Single(await fixture.Service.GetMineAsync(owner, deleted: false));
        Assert.Equal(entryId, active.Id);
        Assert.Equal(Convert.ToBase64String(encryptedPayload), active.Ciphertext);
        Assert.Equal(1, active.Version);

        var replacement = Bytes(96, 77);
        await fixture.Service.UpdateAsync(owner, entryId, Entry(replacement, active.Version));
        active = Assert.Single(await fixture.Service.GetMineAsync(owner, deleted: false));
        Assert.Equal(2, active.Version);
        Assert.Equal(Convert.ToBase64String(replacement), active.Ciphertext);

        await fixture.Service.SoftDeleteAsync(owner, entryId, active.Version);
        Assert.Empty(await fixture.Service.GetMineAsync(owner, deleted: false));
        var deleted = Assert.Single(await fixture.Service.GetMineAsync(owner, deleted: true));
        Assert.Equal(3, deleted.Version);

        await fixture.Service.RestoreAsync(owner, entryId, deleted.Version);
        active = Assert.Single(await fixture.Service.GetMineAsync(owner, deleted: false));
        Assert.Equal(4, active.Version);
        await fixture.Service.SoftDeleteAsync(owner, entryId, active.Version);
        deleted = Assert.Single(await fixture.Service.GetMineAsync(owner, deleted: true));
        await fixture.Service.PermanentlyDiscardAsync(owner, entryId, deleted.Version);
        Assert.Empty(await fixture.Service.GetMineAsync(owner, deleted: true));

        await using var verify = fixture.Context();
        var audits = await verify.AuditEntries.Where(x => x.EntityType == "PrivateJournalMetadata").ToListAsync();
        Assert.Equal(7, audits.Count);
        Assert.All(audits, audit =>
        {
            Assert.DoesNotContain(plaintextSentinel, audit.NewValuesJson ?? "", StringComparison.Ordinal);
            Assert.DoesNotContain(Convert.ToBase64String(encryptedPayload), audit.NewValuesJson ?? "", StringComparison.Ordinal);
            Assert.DoesNotContain(Convert.ToBase64String(replacement), audit.NewValuesJson ?? "", StringComparison.Ordinal);
            Assert.Contains("cryptoVersion", audit.NewValuesJson ?? "", StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task Every_role_is_confined_to_its_own_vault_and_admin_has_no_bypass()
    {
        var fixture = await Fixture.CreateAsync();
        var owner = Principal("waiter-id", "waiter", "Waiter");
        var manager = Principal("manager-id", "manager", "Manager");
        var admin = Principal("admin-id", "admin", "Admin");
        await fixture.Service.SaveKeyEnvelopeAsync(owner, KeyEnvelope());
        var id = await fixture.Service.CreateAsync(owner, Entry(Bytes(64, 9)));

        Assert.Null(await fixture.Service.GetKeyEnvelopeAsync(manager));
        Assert.Null(await fixture.Service.GetKeyEnvelopeAsync(admin));
        Assert.Empty(await fixture.Service.GetMineAsync(manager, false));
        Assert.Empty(await fixture.Service.GetMineAsync(admin, false));
        await Assert.ThrowsAsync<DomainException>(() => fixture.Service.UpdateAsync(manager, id, Entry(Bytes(64, 10), 1)));
        await Assert.ThrowsAsync<DomainException>(() => fixture.Service.SoftDeleteAsync(admin, id, 1));

        var ownerEntry = Assert.Single(await fixture.Service.GetMineAsync(owner, false));
        Assert.Equal(id, ownerEntry.Id);
    }

    [Fact]
    public async Task Invalid_crypto_envelopes_stale_versions_and_inactive_accounts_are_rejected()
    {
        var fixture = await Fixture.CreateAsync();
        var owner = Principal("waiter-id", "waiter");

        await Assert.ThrowsAsync<DomainException>(() => fixture.Service.SaveKeyEnvelopeAsync(owner,
            KeyEnvelope(kdf: 1)));
        await Assert.ThrowsAsync<DomainException>(() => fixture.Service.SaveKeyEnvelopeAsync(owner,
            KeyEnvelope(cryptoVersion: 2)));
        await Assert.ThrowsAsync<DomainException>(() => fixture.Service.SaveKeyEnvelopeAsync(owner,
            KeyEnvelope(passphraseNonce: Convert.ToBase64String(Bytes(11, 1)))));

        await fixture.Service.SaveKeyEnvelopeAsync(owner, KeyEnvelope());
        var envelope = await fixture.Service.GetKeyEnvelopeAsync(owner);
        Assert.NotNull(envelope);
        await Assert.ThrowsAsync<DomainException>(() => fixture.Service.SaveKeyEnvelopeAsync(owner,
            KeyEnvelope(expectedVersion: 999)));
        await Assert.ThrowsAsync<DomainException>(() => fixture.Service.CreateAsync(owner,
            Entry("not-base64", Convert.ToBase64String(Bytes(12, 2)))));
        await Assert.ThrowsAsync<DomainException>(() => fixture.Service.CreateAsync(owner,
            Entry(Convert.ToBase64String(Bytes(16, 2)), Convert.ToBase64String(Bytes(12, 2)))));

        await using (var db = fixture.Context())
        {
            var user = await db.Users.SingleAsync(x => x.Id == "waiter-id");
            user.IsActive = false;
            await db.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<DomainException>(() => fixture.Service.GetMineAsync(owner, false));
    }

    [Fact]
    public async Task Encrypted_rows_survive_logical_backup_and_restore_byte_for_byte()
    {
        var source = await Fixture.CreateAsync("journal-source");
        var owner = Principal("waiter-id", "waiter");
        await source.Service.SaveKeyEnvelopeAsync(owner, KeyEnvelope());
        await source.Service.CreateAsync(owner, Entry(Bytes(128, 93)));

        JournalKeyEnvelope key;
        JournalEntry entry;
        await using (var db = source.Context())
        {
            key = await db.JournalKeyEnvelopes.AsNoTracking().SingleAsync();
            entry = await db.JournalEntries.AsNoTracking().SingleAsync();
        }

        var restored = await Fixture.CreateAsync("journal-restored");
        await using (var db = restored.Context())
        {
            db.JournalKeyEnvelopes.Add(Clone(key));
            db.JournalEntries.Add(Clone(entry));
            await db.SaveChangesAsync();
        }

        var restoredKey = await restored.Service.GetKeyEnvelopeAsync(owner);
        var restoredEntry = Assert.Single(await restored.Service.GetMineAsync(owner, false));
        Assert.NotNull(restoredKey);
        Assert.Equal(Convert.ToBase64String(key.PassphraseWrappedKey), restoredKey.PassphraseWrappedKey);
        Assert.Equal(Convert.ToBase64String(key.RecoveryWrappedKey), restoredKey.RecoveryWrappedKey);
        Assert.Equal(Convert.ToBase64String(entry.Ciphertext), restoredEntry.Ciphertext);
        Assert.Equal(Convert.ToBase64String(entry.Nonce), restoredEntry.Nonce);
        Assert.Equal(entry.Version, restoredEntry.Version);
        Assert.Equal(entry.UpdatedUtc, restoredEntry.UpdatedUtc);
    }

    private static JournalKeyEnvelopeWrite KeyEnvelope(int kdf = 600_000, int cryptoVersion = 1,
        string? passphraseNonce = null, long? expectedVersion = null) => new(
        Convert.ToBase64String(Bytes(16, 1)),
        passphraseNonce ?? Convert.ToBase64String(Bytes(12, 2)),
        Convert.ToBase64String(Bytes(48, 3)),
        Convert.ToBase64String(Bytes(12, 4)),
        Convert.ToBase64String(Bytes(48, 5)),
        kdf, cryptoVersion, expectedVersion);

    private static JournalEntryWrite Entry(byte[] ciphertext, long? expectedVersion = null) =>
        Entry(Convert.ToBase64String(ciphertext), Convert.ToBase64String(Bytes(12, 6)), expectedVersion);

    private static JournalEntryWrite Entry(string ciphertext, string nonce, long? expectedVersion = null) =>
        new(ciphertext, nonce, 1, expectedVersion);

    private static byte[] Bytes(int count, byte seed) =>
        Enumerable.Range(0, count).Select(i => (byte)(seed + i)).ToArray();

    private static string FindJournalModule()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "Roms.Web", "wwwroot", "js", "arcworks-journal.js");
            if (File.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }
        throw new FileNotFoundException("The ARCWorks journal module was not found from the test output path.");
    }

    private static ClaimsPrincipal Principal(string id, string name, string? role = null)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, id), new(ClaimTypes.Name, name) };
        if (role is not null) claims.Add(new Claim(ClaimTypes.Role, role));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    private static JournalKeyEnvelope Clone(JournalKeyEnvelope value) => new()
    {
        UserId = value.UserId,
        PassphraseSalt = [.. value.PassphraseSalt],
        PassphraseNonce = [.. value.PassphraseNonce],
        PassphraseWrappedKey = [.. value.PassphraseWrappedKey],
        RecoveryNonce = [.. value.RecoveryNonce],
        RecoveryWrappedKey = [.. value.RecoveryWrappedKey],
        KdfIterations = value.KdfIterations,
        CryptoVersion = value.CryptoVersion,
        CreatedUtc = value.CreatedUtc,
        UpdatedUtc = value.UpdatedUtc,
        Version = value.Version
    };

    private static JournalEntry Clone(JournalEntry value) => new()
    {
        Id = value.Id,
        UserId = value.UserId,
        Ciphertext = [.. value.Ciphertext],
        Nonce = [.. value.Nonce],
        CryptoVersion = value.CryptoVersion,
        CreatedUtc = value.CreatedUtc,
        UpdatedUtc = value.UpdatedUtc,
        DeletedUtc = value.DeletedUtc,
        Version = value.Version
    };

    private sealed class Fixture(DbContextOptions<RomsDbContext> options)
    {
        public JournalService Service { get; } = new(new TestFactory(options), new FixedClock(Now));
        public RomsDbContext Context() => new(options);

        public static async Task<Fixture> CreateAsync(string? name = null)
        {
            var options = new DbContextOptionsBuilder<RomsDbContext>()
                .UseInMemoryDatabase(name ?? $"journal-{Guid.NewGuid():N}").Options;
            await using var db = new RomsDbContext(options);
            db.Users.AddRange(
                User("waiter-id", "waiter"),
                User("manager-id", "manager"),
                User("admin-id", "admin"));
            await db.SaveChangesAsync();
            return new Fixture(options);
        }

        private static ApplicationUser User(string id, string username) => new()
        {
            Id = id,
            UserName = username,
            NormalizedUserName = username.ToUpperInvariant(),
            DisplayName = username,
            IsActive = true
        };
    }

    private sealed class TestFactory(DbContextOptions<RomsDbContext> options) : IDbContextFactory<RomsDbContext>
    {
        public RomsDbContext CreateDbContext() => new(options);
        public Task<RomsDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new RomsDbContext(options));
    }

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }
}
