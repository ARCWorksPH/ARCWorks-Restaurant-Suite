using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Roms.Domain;
using Roms.Infrastructure.Identity;
using Roms.Infrastructure.Persistence;
using Roms.Infrastructure.Services;

namespace Roms.IntegrationTests;

public sealed class AttendanceWorkflowTests : IAsyncLifetime
{
    private DbContextOptions<RomsDbContext> options = default!;
    private readonly TestClock clock = new() { UtcNow = new(2026, 7, 14, 1, 0, 0, DateTimeKind.Utc) };

    public async Task InitializeAsync()
    {
        options = new DbContextOptionsBuilder<RomsDbContext>()
            .UseInMemoryDatabase($"roms-attendance-{Guid.NewGuid()}")
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options;
        await using var db = new RomsDbContext(options);
        await db.Database.EnsureCreatedAsync();
        db.Users.Add(new ApplicationUser { Id = "waiter-id", UserName = "waiter", NormalizedUserName = "WAITER", DisplayName = "Waiter One", IsActive = true });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Staff_can_clock_in_and_out_against_their_schedule()
    {
        var service = CreateService();
        await service.SaveScheduleAsync(null, "waiter-id", clock.UtcNow, clock.UtcNow.AddHours(8), "Day shift", "admin");
        await service.ClockInAsync("waiter");

        var mine = await service.GetMineAsync("waiter", clock.UtcNow.AddDays(-1), clock.UtcNow.AddDays(2));
        Assert.NotNull(mine.OpenRecord);
        Assert.Equal("Day shift", mine.Schedules.Single().Notes);
        await Assert.ThrowsAsync<DomainException>(() => service.ClockInAsync("waiter"));

        clock.UtcNow = clock.UtcNow.AddHours(8);
        await service.ClockOutAsync("waiter");
        var admin = await service.GetAdminViewAsync(clock.UtcNow.AddDays(-1), clock.UtcNow.AddDays(1));
        Assert.Empty(admin.Present);
        Assert.Equal(8m, admin.Records.Single().Hours);
    }

    [Fact]
    public async Task Overlapping_schedules_and_unexplained_corrections_are_rejected()
    {
        var service = CreateService();
        await service.SaveScheduleAsync(null, "waiter-id", clock.UtcNow, clock.UtcNow.AddHours(8), null, "admin");
        await Assert.ThrowsAsync<DomainException>(() => service.SaveScheduleAsync(null, "waiter-id", clock.UtcNow.AddHours(4), clock.UtcNow.AddHours(10), null, "admin"));
        await service.ClockInAsync("waiter");
        var record = (await service.GetMineAsync("waiter", clock.UtcNow.AddDays(-1), clock.UtcNow.AddDays(1))).OpenRecord!;
        await Assert.ThrowsAsync<DomainException>(() => service.CorrectAsync(record.Id, record.ClockInUtc, record.ClockInUtc.AddHours(1), "", "admin"));
    }

    public Task DisposeAsync() => Task.CompletedTask;
    private AttendanceService CreateService() => new(new TestFactory(options), clock);

    private sealed class TestFactory(DbContextOptions<RomsDbContext> options) : IDbContextFactory<RomsDbContext>
    {
        public RomsDbContext CreateDbContext() => new(options);
        public Task<RomsDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(new RomsDbContext(options));
    }

    private sealed class TestClock : Roms.Application.IClock { public DateTime UtcNow { get; set; } }
}
