using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Roms.Application;
using Roms.Domain;
using Roms.Infrastructure.Identity;
using Roms.Infrastructure.Persistence;
using Roms.Infrastructure.Services;

namespace Roms.IntegrationTests;

public sealed class LeaveRequestTests : IAsyncLifetime
{
    private DbContextOptions<RomsDbContext> options = default!;
    private readonly FixedRestaurantClock clock = new(new DateTime(2026, 8, 23, 4, 0, 0, DateTimeKind.Utc));

    public async Task InitializeAsync()
    {
        options = new DbContextOptionsBuilder<RomsDbContext>()
            .UseInMemoryDatabase($"roms-leave-requests-{Guid.NewGuid():N}")
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        await using var db = new RomsDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var waiterRole = Role(RomsRoles.Waiter);
        var kitchenRole = Role(RomsRoles.Kitchen);
        var managerRole = Role(RomsRoles.Manager);
        var adminRole = Role(RomsRoles.Admin);
        db.Roles.AddRange(waiterRole, kitchenRole, managerRole, adminRole);
        var waiterOne = User("waiter-1", "waiter-one", "Waiter One");
        var waiterTwo = User("waiter-2", "waiter-two", "Waiter Two");
        var kitchen = User("kitchen-1", "kitchen-one", "Kitchen One");
        var manager = User("manager-1", "manager-one", "Manager One");
        var admin = User("admin-1", "admin-one", "Admin One");
        db.Users.AddRange(waiterOne, waiterTwo, kitchen, manager, admin);
        db.UserRoles.AddRange(
            UserRole(waiterOne.Id, waiterRole.Id), UserRole(waiterTwo.Id, waiterRole.Id),
            UserRole(kitchen.Id, kitchenRole.Id), UserRole(manager.Id, managerRole.Id),
            UserRole(admin.Id, adminRole.Id));
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Employee_submits_and_reads_only_own_future_request_without_leaking_private_message_to_audit()
    {
        var service = Service();
        var dates = new[] { clock.LocalDate.AddDays(3), clock.LocalDate.AddDays(2), clock.LocalDate.AddDays(3) };
        var id = await service.SubmitAsync(WaiterOne(), dates, "Vacation", "Private family matter.");

        var mine = Assert.Single(await service.GetMineAsync(WaiterOne()));
        Assert.Equal(id, mine.Id);
        Assert.Equal(new[] { clock.LocalDate.AddDays(2), clock.LocalDate.AddDays(3) }, mine.RequestedDates);
        Assert.Equal("Vacation", mine.LeaveType);
        Assert.Equal("Private family matter.", mine.PrivateMessage);
        Assert.Equal(LeaveRequestStatus.Pending, mine.Status);
        Assert.Empty(await service.GetMineAsync(WaiterTwo()));

        await using var db = new RomsDbContext(options);
        var audit = Assert.Single(db.AuditEntries);
        Assert.Equal("SubmitLeaveRequest", audit.Action);
        Assert.Equal(nameof(LeaveRequest), audit.EntityType);
        Assert.DoesNotContain("Private family matter", audit.NewValuesJson, StringComparison.Ordinal);
        Assert.Contains("HasPrivateMessage", audit.NewValuesJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Invalid_dates_and_overlapping_pending_or_approved_dates_are_rejected()
    {
        var service = Service();
        await Assert.ThrowsAsync<DomainException>(() => service.SubmitAsync(WaiterOne(), [], null, null));
        await Assert.ThrowsAsync<DomainException>(() => service.SubmitAsync(WaiterOne(), [clock.LocalDate], null, null));
        await Assert.ThrowsAsync<DomainException>(() => service.SubmitAsync(WaiterOne(), [clock.LocalDate.AddDays(-1)], null, null));

        var sharedDate = clock.LocalDate.AddDays(5);
        var firstId = await service.SubmitAsync(WaiterOne(), [sharedDate], null, null);
        await Assert.ThrowsAsync<DomainException>(() =>
            service.SubmitAsync(WaiterOne(), [sharedDate, sharedDate.AddDays(1)], null, null));

        var first = Assert.Single(await service.GetMineAsync(WaiterOne()));
        await service.DecideAsync(Manager(), firstId, first.Version, true, null);
        await Assert.ThrowsAsync<DomainException>(() => service.SubmitAsync(WaiterOne(), [sharedDate], null, null));

        await service.SubmitAsync(WaiterTwo(), [sharedDate], null, null);
        Assert.Single(await service.GetMineAsync(WaiterTwo()));
    }

    [Fact]
    public async Task Employee_can_edit_or_cancel_only_own_eligible_pending_request_with_current_version()
    {
        var service = Service();
        var id = await service.SubmitAsync(WaiterOne(), [clock.LocalDate.AddDays(4)], "Personal", "Original");
        var original = Assert.Single(await service.GetMineAsync(WaiterOne()));

        await Assert.ThrowsAsync<DomainException>(() => service.UpdateAsync(WaiterTwo(), id, original.Version,
            [clock.LocalDate.AddDays(6)], null, null));
        await Assert.ThrowsAsync<DomainException>(() => service.CancelAsync(WaiterTwo(), id, original.Version));

        await service.UpdateAsync(WaiterOne(), id, original.Version, [clock.LocalDate.AddDays(6)], "Medical", "Updated");
        var updated = Assert.Single(await service.GetMineAsync(WaiterOne()));
        Assert.Equal(original.Version + 1, updated.Version);
        Assert.Equal("Medical", updated.LeaveType);
        Assert.NotNull(updated.ChangedLocal);
        await Assert.ThrowsAsync<DomainException>(() => service.CancelAsync(WaiterOne(), id, original.Version));

        await service.CancelAsync(WaiterOne(), id, updated.Version);
        var cancelled = Assert.Single(await service.GetMineAsync(WaiterOne()));
        Assert.Equal(LeaveRequestStatus.Cancelled, cancelled.Status);
        Assert.NotNull(cancelled.CancelledLocal);
        await Assert.ThrowsAsync<DomainException>(() => service.UpdateAsync(WaiterOne(), id, cancelled.Version,
            [clock.LocalDate.AddDays(7)], null, null));

        // A cancelled request releases its date for a new request.
        await service.SubmitAsync(WaiterOne(), [clock.LocalDate.AddDays(6)], null, null);
        Assert.Equal(2, (await service.GetMineAsync(WaiterOne())).Count);
    }

    [Fact]
    public async Task Manager_or_admin_can_decide_pending_request_without_rewriting_schedules()
    {
        var service = Service();
        var approvedId = await service.SubmitAsync(WaiterOne(), [clock.LocalDate.AddDays(8)], "Vacation", null);
        var approved = Assert.Single(await service.GetMineAsync(WaiterOne()));
        await Assert.ThrowsAsync<DomainException>(() => service.DecideAsync(Kitchen(), approvedId, approved.Version, true, null));
        await service.DecideAsync(Manager(), approvedId, approved.Version, true, "Coverage confirmed.");

        var declinedId = await service.SubmitAsync(WaiterTwo(), [clock.LocalDate.AddDays(9)], null, "Please review.");
        var declined = Assert.Single(await service.GetMineAsync(WaiterTwo()));
        await Assert.ThrowsAsync<DomainException>(() => service.DecideAsync(Admin(), declinedId, declined.Version, false, null));
        await service.DecideAsync(Admin(), declinedId, declined.Version, false, "Insufficient coverage.");

        var decisions = await service.GetForDecisionAsync(Manager(), null);
        Assert.Equal(2, decisions.Count);
        Assert.Equal(LeaveRequestStatus.Approved, decisions.Single(x => x.Id == approvedId).Status);
        Assert.Equal(LeaveRequestStatus.Declined, decisions.Single(x => x.Id == declinedId).Status);

        await using var db = new RomsDbContext(options);
        Assert.Empty(db.StaffSchedules);
        Assert.Contains(db.AuditEntries, x => x.Action == "ApproveLeaveRequest" && x.ActorId == "manager-one");
        Assert.Contains(db.AuditEntries, x => x.Action == "DeclineLeaveRequest" && x.ActorId == "admin-one");
    }

    [Fact]
    public async Task Invalid_transitions_self_decision_stale_version_and_inactive_identity_are_denied()
    {
        var service = Service();
        var managerRequestId = await service.SubmitAsync(Manager(), [clock.LocalDate.AddDays(10)], null, null);
        var managerRequest = Assert.Single(await service.GetMineAsync(Manager()));
        await Assert.ThrowsAsync<DomainException>(() =>
            service.DecideAsync(Manager(), managerRequestId, managerRequest.Version, true, null));
        await service.DecideAsync(Admin(), managerRequestId, managerRequest.Version, true, null);
        var decided = Assert.Single(await service.GetMineAsync(Manager()));
        await Assert.ThrowsAsync<DomainException>(() =>
            service.DecideAsync(Admin(), managerRequestId, decided.Version, true, null));
        await Assert.ThrowsAsync<DomainException>(() => service.CancelAsync(Manager(), managerRequestId, decided.Version));

        var waiterId = await service.SubmitAsync(WaiterOne(), [clock.LocalDate.AddDays(11)], null, null);
        var waiterRequest = Assert.Single(await service.GetMineAsync(WaiterOne()), x => x.Id == waiterId);
        await service.UpdateAsync(WaiterOne(), waiterId, waiterRequest.Version, [clock.LocalDate.AddDays(12)], null, null);
        await Assert.ThrowsAsync<DomainException>(() => service.UpdateAsync(WaiterOne(), waiterId, waiterRequest.Version,
            [clock.LocalDate.AddDays(13)], null, null));

        await using (var db = new RomsDbContext(options))
        {
            (await db.Users.FindAsync("waiter-1"))!.IsActive = false;
            await db.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<DomainException>(() => service.GetMineAsync(WaiterOne()));
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private LeaveRequestService Service() => new(new TestFactory(options), clock);
    private static ClaimsPrincipal WaiterOne() => Principal("waiter-1", "waiter-one");
    private static ClaimsPrincipal WaiterTwo() => Principal("waiter-2", "waiter-two");
    private static ClaimsPrincipal Kitchen() => Principal("kitchen-1", "kitchen-one");
    private static ClaimsPrincipal Manager() => Principal("manager-1", "manager-one");
    private static ClaimsPrincipal Admin() => Principal("admin-1", "admin-one");

    private static ClaimsPrincipal Principal(string id, string username) => new(new ClaimsIdentity(new[]
    {
        new Claim(ClaimTypes.NameIdentifier, id), new Claim(ClaimTypes.Name, username)
    }, "Gate2FTest"));

    private static ApplicationUser User(string id, string username, string displayName) => new()
    {
        Id = id,
        UserName = username,
        NormalizedUserName = username.ToUpperInvariant(),
        DisplayName = displayName,
        IsActive = true
    };

    private static IdentityRole Role(string name) => new(name)
    {
        Id = $"{name.ToLowerInvariant()}-role",
        NormalizedName = name.ToUpperInvariant()
    };

    private static IdentityUserRole<string> UserRole(string userId, string roleId) => new()
    {
        UserId = userId,
        RoleId = roleId
    };

    private sealed class TestFactory(DbContextOptions<RomsDbContext> options) : IDbContextFactory<RomsDbContext>
    {
        public RomsDbContext CreateDbContext() => new(options);
        public Task<RomsDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new RomsDbContext(options));
    }

    private sealed class FixedRestaurantClock(DateTime utcNow) : IRestaurantClock
    {
        private static readonly TimeZoneInfo Manila = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
        public DateTime UtcNow { get; } = utcNow;
        public DateTime LocalNow => ToLocal(UtcNow);
        public DateOnly LocalDate => DateOnly.FromDateTime(LocalNow);
        public TimeZoneInfo TimeZone => Manila;
        public DateTime ToLocal(DateTime utcInstant) => TimeZoneInfo.ConvertTimeFromUtc(utcInstant, Manila);
        public DateTime ToUtc(DateTime localRestaurantTime) => TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(localRestaurantTime, DateTimeKind.Unspecified), Manila);
        public DateOnly StartOfWeek(DateOnly localDate) => localDate.AddDays(-(((int)localDate.DayOfWeek + 6) % 7));
    }
}
