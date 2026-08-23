using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Roms.Application;
using Roms.Domain;
using Roms.Infrastructure.Identity;
using Roms.Infrastructure.Services;

namespace Roms.IntegrationTests;

[Collection(MariaDbCollection.Name)]
public sealed class MariaDbPrivateJournalTests(MariaDbFixture fixture)
{
    [Fact]
    public async Task Binary_envelopes_persist_and_concurrent_updates_commit_exactly_once()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using (var seed = database.CreateContext())
        {
            seed.Users.Add(new ApplicationUser
            {
                Id = "journal-owner",
                UserName = "journal-owner",
                NormalizedUserName = "JOURNAL-OWNER",
                DisplayName = "Journal Owner",
                IsActive = true
            });
            await seed.SaveChangesAsync();
        }

        var clock = new FixedClock(new DateTime(2026, 8, 23, 5, 0, 0, DateTimeKind.Utc));
        var actor = Principal();
        var service = new JournalService(database.CreateFactory(), clock);
        await service.SaveKeyEnvelopeAsync(actor, Envelope());
        var entryId = await service.CreateAsync(actor, Entry(21));
        var expectedVersion = Assert.Single(await service.GetMineAsync(actor, false)).Version;

        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = UpdateAsync(start.Task, new JournalService(database.CreateFactory(), clock), actor,
            entryId, Entry(31, expectedVersion));
        var second = UpdateAsync(start.Task, new JournalService(database.CreateFactory(), clock), actor,
            entryId, Entry(41, expectedVersion));
        start.SetResult();
        var results = await Task.WhenAll(first, second);

        Assert.Single(results, result => result is null);
        Assert.Single(results, result => result is DomainException);
        await using var verify = database.CreateContext();
        var saved = await verify.JournalEntries.AsNoTracking().SingleAsync(x => x.Id == entryId);
        Assert.Equal(expectedVersion + 1, saved.Version);
        Assert.Equal(12, saved.Nonce.Length);
        Assert.Equal(80, saved.Ciphertext.Length);
        Assert.Equal(1, await verify.AuditEntries.CountAsync(x =>
            x.EntityType == "PrivateJournalMetadata" && x.EntityId == entryId.ToString() &&
            x.Action == "UpdateJournalEntry"));
    }

    private static async Task<Exception?> UpdateAsync(Task start, JournalService service, ClaimsPrincipal actor,
        Guid id, JournalEntryWrite value)
    {
        await start;
        try { await service.UpdateAsync(actor, id, value); return null; }
        catch (DomainException exception) { return exception; }
    }

    private static ClaimsPrincipal Principal() => new(new ClaimsIdentity(new[]
    {
        new Claim(ClaimTypes.NameIdentifier, "journal-owner"), new Claim(ClaimTypes.Name, "journal-owner")
    }, "Gate2GMariaDbTest"));

    private static JournalKeyEnvelopeWrite Envelope() => new(
        Convert.ToBase64String(Data(16, 1)), Convert.ToBase64String(Data(12, 2)),
        Convert.ToBase64String(Data(48, 3)), Convert.ToBase64String(Data(12, 4)),
        Convert.ToBase64String(Data(48, 5)), 600_000, 1, null);

    private static JournalEntryWrite Entry(byte seed, long? version = null) => new(
        Convert.ToBase64String(Data(80, seed)), Convert.ToBase64String(Data(12, (byte)(seed + 1))), 1, version);

    private static byte[] Data(int count, byte seed) =>
        Enumerable.Range(0, count).Select(index => (byte)(seed + index)).ToArray();

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }
}
