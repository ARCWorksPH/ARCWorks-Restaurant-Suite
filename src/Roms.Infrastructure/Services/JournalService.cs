using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Roms.Application;
using Roms.Domain;
using Roms.Infrastructure.Persistence;

namespace Roms.Infrastructure.Services;

public sealed class JournalService(IDbContextFactory<RomsDbContext> factory, IClock clock) : IJournalService
{
    private const int CurrentCryptoVersion = 1;
    private const int MinimumKdfIterations = 310_000;
    private const int MaximumKdfIterations = 2_000_000;
    private const int MaximumCiphertextBytes = 512 * 1024;

    public async Task<JournalKeyEnvelopeView?> GetKeyEnvelopeAsync(ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var actor = await RequireActiveEmployeeAsync(db, principal, cancellationToken);
        var envelope = await db.JournalKeyEnvelopes.AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == actor.Id, cancellationToken);
        return envelope is null ? null : Map(envelope);
    }

    public async Task SaveKeyEnvelopeAsync(ClaimsPrincipal principal, JournalKeyEnvelopeWrite value,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);
        var decoded = DecodeAndValidateEnvelope(value);
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var actor = await RequireActiveEmployeeAsync(db, principal, cancellationToken);
        var envelope = await db.JournalKeyEnvelopes.SingleOrDefaultAsync(x => x.UserId == actor.Id, cancellationToken);
        var now = clock.UtcNow;
        string action;
        if (envelope is null)
        {
            if (value.ExpectedVersion is not null)
                throw new DomainException("The journal key state changed. Refresh and try again.");
            envelope = new JournalKeyEnvelope { UserId = actor.Id, CreatedUtc = now };
            db.JournalKeyEnvelopes.Add(envelope);
            action = "CreateJournalKeyEnvelope";
        }
        else
        {
            EnsureVersion(envelope.Version, value.ExpectedVersion);
            envelope.Version++;
            action = "RotateJournalKeyEnvelope";
        }

        envelope.PassphraseSalt = decoded.PassphraseSalt;
        envelope.PassphraseNonce = decoded.PassphraseNonce;
        envelope.PassphraseWrappedKey = decoded.PassphraseWrappedKey;
        envelope.RecoveryNonce = decoded.RecoveryNonce;
        envelope.RecoveryWrappedKey = decoded.RecoveryWrappedKey;
        envelope.KdfIterations = value.KdfIterations;
        envelope.CryptoVersion = value.CryptoVersion;
        envelope.UpdatedUtc = now;
        db.AuditEntries.Add(MetadataAudit(actor.UserName, action, actor.Id, envelope.Version, value.CryptoVersion));
        await SaveAsync(db, cancellationToken);
    }

    public async Task<IReadOnlyList<JournalEntryView>> GetMineAsync(ClaimsPrincipal principal, bool deleted,
        CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var actor = await RequireActiveEmployeeAsync(db, principal, cancellationToken);
        var entries = await db.JournalEntries.AsNoTracking()
            .Where(x => x.UserId == actor.Id && (deleted ? x.DeletedUtc != null : x.DeletedUtc == null))
            .OrderByDescending(x => x.UpdatedUtc)
            .ToListAsync(cancellationToken);
        return entries.Select(Map).ToList();
    }

    public async Task<Guid> CreateAsync(ClaimsPrincipal principal, JournalEntryWrite value,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.ExpectedVersion is not null) throw new DomainException("A new journal entry cannot have a prior version.");
        var decoded = DecodeAndValidateEntry(value);
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var actor = await RequireActiveEmployeeAsync(db, principal, cancellationToken);
        await RequireKeyEnvelopeAsync(db, actor.Id, value.CryptoVersion, cancellationToken);
        var now = clock.UtcNow;
        var entry = new JournalEntry
        {
            UserId = actor.Id,
            Ciphertext = decoded.Ciphertext,
            Nonce = decoded.Nonce,
            CryptoVersion = value.CryptoVersion,
            CreatedUtc = now,
            UpdatedUtc = now
        };
        db.JournalEntries.Add(entry);
        db.AuditEntries.Add(MetadataAudit(actor.UserName, "CreateJournalEntry", entry.Id, entry.Version, entry.CryptoVersion));
        await SaveAsync(db, cancellationToken);
        return entry.Id;
    }

    public async Task UpdateAsync(ClaimsPrincipal principal, Guid id, JournalEntryWrite value,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);
        var decoded = DecodeAndValidateEntry(value);
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var actor = await RequireActiveEmployeeAsync(db, principal, cancellationToken);
        var entry = await RequireOwnedEntryAsync(db, actor.Id, id, cancellationToken);
        EnsureVersion(entry.Version, value.ExpectedVersion);
        if (entry.DeletedUtc is not null) throw new DomainException("Restore the journal entry before editing it.");
        await RequireKeyEnvelopeAsync(db, actor.Id, value.CryptoVersion, cancellationToken);
        entry.Ciphertext = decoded.Ciphertext;
        entry.Nonce = decoded.Nonce;
        entry.CryptoVersion = value.CryptoVersion;
        entry.UpdatedUtc = clock.UtcNow;
        entry.Version++;
        db.AuditEntries.Add(MetadataAudit(actor.UserName, "UpdateJournalEntry", entry.Id, entry.Version, entry.CryptoVersion));
        await SaveAsync(db, cancellationToken);
    }

    public Task SoftDeleteAsync(ClaimsPrincipal principal, Guid id, long expectedVersion,
        CancellationToken cancellationToken = default) =>
        ChangeDeletedStateAsync(principal, id, expectedVersion, true, cancellationToken);

    public Task RestoreAsync(ClaimsPrincipal principal, Guid id, long expectedVersion,
        CancellationToken cancellationToken = default) =>
        ChangeDeletedStateAsync(principal, id, expectedVersion, false, cancellationToken);

    public async Task PermanentlyDiscardAsync(ClaimsPrincipal principal, Guid id, long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var actor = await RequireActiveEmployeeAsync(db, principal, cancellationToken);
        var entry = await RequireOwnedEntryAsync(db, actor.Id, id, cancellationToken);
        EnsureVersion(entry.Version, expectedVersion);
        if (entry.DeletedUtc is null) throw new DomainException("Only a deleted journal entry can be permanently discarded.");
        db.JournalEntries.Remove(entry);
        db.AuditEntries.Add(MetadataAudit(actor.UserName, "DiscardJournalEntry", entry.Id, entry.Version, entry.CryptoVersion));
        await SaveAsync(db, cancellationToken);
    }

    private async Task ChangeDeletedStateAsync(ClaimsPrincipal principal, Guid id, long expectedVersion, bool delete,
        CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var actor = await RequireActiveEmployeeAsync(db, principal, cancellationToken);
        var entry = await RequireOwnedEntryAsync(db, actor.Id, id, cancellationToken);
        EnsureVersion(entry.Version, expectedVersion);
        if (delete && entry.DeletedUtc is not null) throw new DomainException("The journal entry is already deleted.");
        if (!delete && entry.DeletedUtc is null) throw new DomainException("The journal entry is not deleted.");
        entry.DeletedUtc = delete ? clock.UtcNow : null;
        entry.UpdatedUtc = clock.UtcNow;
        entry.Version++;
        db.AuditEntries.Add(MetadataAudit(actor.UserName, delete ? "DeleteJournalEntry" : "RestoreJournalEntry",
            entry.Id, entry.Version, entry.CryptoVersion));
        await SaveAsync(db, cancellationToken);
    }

    private static (byte[] PassphraseSalt, byte[] PassphraseNonce, byte[] PassphraseWrappedKey,
        byte[] RecoveryNonce, byte[] RecoveryWrappedKey) DecodeAndValidateEnvelope(JournalKeyEnvelopeWrite value)
    {
        if (value.CryptoVersion != CurrentCryptoVersion) throw new DomainException("The journal encryption version is unsupported.");
        if (value.KdfIterations is < MinimumKdfIterations or > MaximumKdfIterations)
            throw new DomainException("The journal key-derivation settings are unsupported.");
        return (
            Decode(value.PassphraseSalt, 16, 32, "passphrase salt"),
            Decode(value.PassphraseNonce, 12, 12, "passphrase nonce"),
            Decode(value.PassphraseWrappedKey, 48, 64, "passphrase-wrapped key"),
            Decode(value.RecoveryNonce, 12, 12, "recovery nonce"),
            Decode(value.RecoveryWrappedKey, 48, 64, "recovery-wrapped key"));
    }

    private static (byte[] Ciphertext, byte[] Nonce) DecodeAndValidateEntry(JournalEntryWrite value)
    {
        if (value.CryptoVersion != CurrentCryptoVersion) throw new DomainException("The journal encryption version is unsupported.");
        return (Decode(value.Ciphertext, 17, MaximumCiphertextBytes, "journal ciphertext"),
            Decode(value.Nonce, 12, 12, "journal nonce"));
    }

    private static byte[] Decode(string encoded, int minimum, int maximum, string field)
    {
        if (string.IsNullOrWhiteSpace(encoded)) throw new DomainException($"A valid {field} is required.");
        try
        {
            var bytes = Convert.FromBase64String(encoded);
            if (bytes.Length < minimum || bytes.Length > maximum) throw new DomainException($"The {field} has an invalid length.");
            return bytes;
        }
        catch (FormatException)
        {
            throw new DomainException($"The {field} is invalid.");
        }
    }

    private static void EnsureVersion(long current, long? expected)
    {
        if (expected is null || expected < 1 || current != expected)
            throw new DomainException("The journal record changed since it was loaded. Refresh and try again.");
    }

    private static async Task RequireKeyEnvelopeAsync(RomsDbContext db, string userId, int cryptoVersion,
        CancellationToken cancellationToken)
    {
        if (!await db.JournalKeyEnvelopes.AsNoTracking().AnyAsync(
                x => x.UserId == userId && x.CryptoVersion == cryptoVersion, cancellationToken))
            throw new DomainException("Set up and unlock the private journal before saving entries.");
    }

    private static async Task<JournalEntry> RequireOwnedEntryAsync(RomsDbContext db, string userId, Guid id,
        CancellationToken cancellationToken) =>
        await db.JournalEntries.SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken)
        ?? throw new DomainException("Journal entry not found.");

    private static async Task<StaffActor> RequireActiveEmployeeAsync(RomsDbContext db, ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(principal);
        if (principal.Identity?.IsAuthenticated != true)
            throw new DomainException("An authenticated staff identity is required.");
        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(id)) throw new DomainException("An authenticated staff identity is required.");
        return await db.Users.AsNoTracking().Where(x => x.Id == id && x.IsActive && x.UserName != null)
            .Select(x => new StaffActor(x.Id, x.UserName!)).SingleOrDefaultAsync(cancellationToken)
            ?? throw new DomainException("An active staff account is required.");
    }

    private static JournalKeyEnvelopeView Map(JournalKeyEnvelope value) => new(
        Convert.ToBase64String(value.PassphraseSalt), Convert.ToBase64String(value.PassphraseNonce),
        Convert.ToBase64String(value.PassphraseWrappedKey), Convert.ToBase64String(value.RecoveryNonce),
        Convert.ToBase64String(value.RecoveryWrappedKey), value.KdfIterations, value.CryptoVersion, value.Version);

    private static JournalEntryView Map(JournalEntry value) => new(
        value.Id, Convert.ToBase64String(value.Ciphertext), Convert.ToBase64String(value.Nonce), value.CryptoVersion,
        value.CreatedUtc, value.UpdatedUtc, value.DeletedUtc, value.Version);

    private AuditEntry MetadataAudit(string actor, string action, object entityId, long version, int cryptoVersion) => new()
    {
        ActorId = actor,
        Action = action,
        EntityType = "PrivateJournalMetadata",
        EntityId = entityId.ToString() ?? "",
        NewValuesJson = $"{{\"version\":{version},\"cryptoVersion\":{cryptoVersion}}}",
        OccurredUtc = clock.UtcNow
    };

    private static async Task SaveAsync(RomsDbContext db, CancellationToken cancellationToken)
    {
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException)
        {
            throw new DomainException("The journal record changed since it was loaded. Refresh and try again.");
        }
    }

    private sealed record StaffActor(string Id, string UserName);
}
