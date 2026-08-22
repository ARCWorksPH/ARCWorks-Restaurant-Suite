using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Roms.Application;
using Roms.Domain;
using Roms.Infrastructure.Identity;
using Roms.Infrastructure.Services;

namespace Roms.IntegrationTests;

[Collection(MariaDbCollection.Name)]
public sealed class MariaDbLeaveRequestConcurrencyTests(MariaDbFixture fixture)
{
    [Fact]
    public async Task Concurrent_manager_decisions_commit_exactly_one_transition_and_audit()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var clock = new FixedRestaurantClock(new DateTime(2026, 8, 23, 4, 0, 0, DateTimeKind.Utc));
        await SeedIdentityAsync(database);
        var submitter = new LeaveRequestService(database.CreateFactory(), clock);
        var requestId = await submitter.SubmitAsync(Waiter(), [clock.LocalDate.AddDays(7)], "Vacation", "Private.");
        var expectedVersion = Assert.Single(await submitter.GetMineAsync(Waiter())).Version;

        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var approve = DecideAsync(start.Task, new LeaveRequestService(database.CreateFactory(), clock),
            Manager(), requestId, expectedVersion, true, null);
        var decline = DecideAsync(start.Task, new LeaveRequestService(database.CreateFactory(), clock),
            Admin(), requestId, expectedVersion, false, "Coverage changed.");
        start.SetResult();
        var results = await Task.WhenAll(approve, decline);

        Assert.Single(results, x => x is null);
        Assert.Single(results, x => x is DomainException);
        await using var verify = database.CreateContext();
        var saved = await verify.LeaveRequests.SingleAsync(x => x.Id == requestId);
        Assert.True(saved.Status is LeaveRequestStatus.Approved or LeaveRequestStatus.Declined);
        Assert.Equal(expectedVersion + 1, saved.Version);
        Assert.Equal(1, await verify.AuditEntries.CountAsync(x => x.EntityId == requestId.ToString() &&
            (x.Action == "ApproveLeaveRequest" || x.Action == "DeclineLeaveRequest")));
    }

    private static async Task SeedIdentityAsync(MariaDbTestDatabase database)
    {
        await using var db = database.CreateContext();
        var waiterRole = Role(RomsRoles.Waiter);
        var managerRole = Role(RomsRoles.Manager);
        var adminRole = Role(RomsRoles.Admin);
        var waiter = User("waiter-1", "waiter-one");
        var manager = User("manager-1", "manager-one");
        var admin = User("admin-1", "admin-one");
        db.Roles.AddRange(waiterRole, managerRole, adminRole);
        db.Users.AddRange(waiter, manager, admin);
        db.UserRoles.AddRange(UserRole(waiter.Id, waiterRole.Id), UserRole(manager.Id, managerRole.Id),
            UserRole(admin.Id, adminRole.Id));
        await db.SaveChangesAsync();
    }

    private static async Task<Exception?> DecideAsync(Task start, LeaveRequestService service, ClaimsPrincipal actor,
        Guid requestId, long expectedVersion, bool approve, string? reason)
    {
        await start;
        try
        {
            await service.DecideAsync(actor, requestId, expectedVersion, approve, reason);
            return null;
        }
        catch (DomainException exception)
        {
            return exception;
        }
    }

    private static ClaimsPrincipal Waiter() => Principal("waiter-1", "waiter-one");
    private static ClaimsPrincipal Manager() => Principal("manager-1", "manager-one");
    private static ClaimsPrincipal Admin() => Principal("admin-1", "admin-one");

    private static ClaimsPrincipal Principal(string id, string username) => new(new ClaimsIdentity(new[]
    {
        new Claim(ClaimTypes.NameIdentifier, id), new Claim(ClaimTypes.Name, username)
    }, "Gate2FMariaDbTest"));

    private static ApplicationUser User(string id, string username) => new()
    {
        Id = id,
        UserName = username,
        NormalizedUserName = username.ToUpperInvariant(),
        DisplayName = username,
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
