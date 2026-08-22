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

public sealed class StaffCommunicationTests : IAsyncLifetime
{
    private DbContextOptions<RomsDbContext> options = default!;
    private readonly FixedRestaurantClock clock = new(new DateTime(2026, 8, 23, 4, 0, 0, DateTimeKind.Utc));

    public async Task InitializeAsync()
    {
        options = new DbContextOptionsBuilder<RomsDbContext>()
            .UseInMemoryDatabase($"roms-staff-communication-{Guid.NewGuid():N}")
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        await using var db = new RomsDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var waiterRole = Role(RomsRoles.Waiter);
        var kitchenRole = Role(RomsRoles.Kitchen);
        var managerRole = Role(RomsRoles.Manager);
        var adminRole = Role(RomsRoles.Admin);
        db.Roles.AddRange(waiterRole, kitchenRole, managerRole, adminRole);
        var waiterOne = User("waiter-1", "waiter-one");
        var waiterTwo = User("waiter-2", "waiter-two");
        var kitchen = User("kitchen-1", "kitchen-one");
        var manager = User("manager-1", "manager-one");
        var admin = User("admin-1", "admin-one");
        db.Users.AddRange(waiterOne, waiterTwo, kitchen, manager, admin);
        db.UserRoles.AddRange(
            UserRole(waiterOne.Id, waiterRole.Id), UserRole(waiterTwo.Id, waiterRole.Id),
            UserRole(kitchen.Id, kitchenRole.Id), UserRole(manager.Id, managerRole.Id),
            UserRole(admin.Id, adminRole.Id));

        var schedule = new StaffSchedule { UserId = waiterOne.Id, CreatedBy = "admin-one" };
        schedule.SetSchedule(
            clock.ToUtc(new DateTime(2026, 8, 23, 8, 0, 0)),
            clock.ToUtc(new DateTime(2026, 8, 23, 17, 0, 0)),
            "Welcome the anniversary table near the window.", clock.UtcNow);
        db.StaffSchedules.Add(schedule);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Staff_hub_uses_current_schedule_note_and_delivers_only_live_matching_audience()
    {
        var service = Service();
        await service.CreateAnnouncementAsync(Manager(), "All staff", "General briefing.",
            StaffAnnouncementPriority.Normal, null, clock.UtcNow.AddMinutes(-5), clock.UtcNow.AddHours(2));
        await service.CreateAnnouncementAsync(Manager(), "Waiters", "Dining room briefing.",
            StaffAnnouncementPriority.Important, RomsRoles.Waiter, clock.UtcNow.AddMinutes(-4), null);
        await service.CreateAnnouncementAsync(Manager(), "Kitchen", "Prep briefing.",
            StaffAnnouncementPriority.Normal, RomsRoles.Kitchen, clock.UtcNow.AddMinutes(-3), null);
        await service.CreateAnnouncementAsync(Manager(), "Future", "Not published yet.",
            StaffAnnouncementPriority.Normal, null, clock.UtcNow.AddMinutes(1), null);
        await service.CreateAnnouncementAsync(Manager(), "Expired", "No longer current.",
            StaffAnnouncementPriority.Normal, null, clock.UtcNow.AddHours(-2), clock.UtcNow.AddMinutes(-1));
        var inactiveId = await service.CreateAnnouncementAsync(Manager(), "Inactive", "Not currently active.",
            StaffAnnouncementPriority.Normal, null, clock.UtcNow.AddMinutes(-2), null);
        await service.SetAnnouncementActiveAsync(Manager(), inactiveId, false);

        var hub = await service.GetStaffHubAsync(WaiterOne());

        Assert.Equal("Welcome the anniversary table near the window.", hub.ManagerNote);
        Assert.Equal(new[] { "Waiters", "All staff" }, hub.Announcements.Select(x => x.Title));
        Assert.Equal(StaffAnnouncementPriority.Important, hub.Announcements[0].Priority);
        Assert.All(hub.Announcements, x => Assert.False(x.RequiresAcknowledgment));
    }

    [Fact]
    public async Task Dismissal_is_per_employee_and_never_deletes_the_source()
    {
        var service = Service();
        var id = await service.CreateAnnouncementAsync(Admin(), "Service reminder", "Check table notes.",
            StaffAnnouncementPriority.Normal, RomsRoles.Waiter, clock.UtcNow, null);
        var version = (await service.GetStaffHubAsync(WaiterOne())).Announcements.Single().Version;

        await service.DismissAsync(WaiterOne(), id, version);

        Assert.Empty((await service.GetStaffHubAsync(WaiterOne())).Announcements);
        Assert.Single((await service.GetStaffHubAsync(WaiterTwo())).Announcements);
        await using var db = new RomsDbContext(options);
        Assert.NotNull(await db.StaffAnnouncements.FindAsync(id));
        Assert.Single(db.StaffAnnouncementReceipts);
    }

    [Fact]
    public async Task Urgent_notice_requires_acknowledgment_and_edit_creates_a_fresh_version()
    {
        var service = Service();
        var id = await service.CreateAnnouncementAsync(Manager(), "Emergency exit", "Use the north exit.",
            StaffAnnouncementPriority.Urgent, RomsRoles.Waiter, clock.UtcNow, null);
        var first = (await service.GetStaffHubAsync(WaiterOne())).Announcements.Single();
        Assert.True(first.RequiresAcknowledgment);
        Assert.False(first.IsAcknowledged);
        await Assert.ThrowsAsync<DomainException>(() => service.DismissAsync(WaiterOne(), id, first.Version));

        await service.AcknowledgeAsync(WaiterOne(), id, first.Version);
        Assert.True((await service.GetStaffHubAsync(WaiterOne())).Announcements.Single().IsAcknowledged);
        await service.DismissAsync(WaiterOne(), id, first.Version);
        Assert.Empty((await service.GetStaffHubAsync(WaiterOne())).Announcements);

        await service.UpdateAnnouncementAsync(Manager(), id, "Emergency exit updated", "Use the south exit.",
            StaffAnnouncementPriority.Urgent, RomsRoles.Waiter, clock.UtcNow, null);
        var edited = (await service.GetStaffHubAsync(WaiterOne())).Announcements.Single();
        Assert.Equal(first.Version + 1, edited.Version);
        Assert.False(edited.IsAcknowledged);
        Assert.Equal("Emergency exit updated", edited.Title);
        await Assert.ThrowsAsync<DomainException>(() => service.AcknowledgeAsync(WaiterOne(), id, first.Version));
    }

    [Fact]
    public async Task Authorization_identity_and_validation_are_enforced_and_actions_are_audited()
    {
        var service = Service();
        await Assert.ThrowsAsync<DomainException>(() => service.CreateAnnouncementAsync(WaiterOne(), "No", "Not allowed.",
            StaffAnnouncementPriority.Normal, null, clock.UtcNow, null));
        await Assert.ThrowsAsync<DomainException>(() => service.GetStaffHubAsync(Manager()));
        await Assert.ThrowsAsync<DomainException>(() => service.CreateAnnouncementAsync(Manager(), "Bad audience", "No.",
            StaffAnnouncementPriority.Normal, "Owner", clock.UtcNow, null));
        await Assert.ThrowsAsync<DomainException>(() => service.CreateAnnouncementAsync(Manager(), "Local time", "No.",
            StaffAnnouncementPriority.Normal, null, DateTime.SpecifyKind(clock.UtcNow, DateTimeKind.Unspecified), null));

        var id = await service.CreateAnnouncementAsync(Manager(), "Urgent", "Please acknowledge.",
            StaffAnnouncementPriority.Urgent, RomsRoles.Waiter, clock.UtcNow, null);
        var version = (await service.GetStaffHubAsync(WaiterOne())).Announcements.Single().Version;
        await service.AcknowledgeAsync(WaiterOne(), id, version);
        await service.DismissAsync(WaiterOne(), id, version);

        await using var db = new RomsDbContext(options);
        var actions = await db.AuditEntries.OrderBy(x => x.Id).Select(x => x.Action).ToListAsync();
        Assert.Contains("CreateStaffAnnouncement", actions);
        Assert.Contains("AcknowledgeStaffAnnouncement", actions);
        Assert.Contains("DismissStaffAnnouncement", actions);
        Assert.All(db.AuditEntries, entry => Assert.Equal(nameof(StaffAnnouncement), entry.EntityType));
    }

    [Fact]
    public async Task Normal_notice_cannot_be_acknowledged_and_inactive_waiter_is_denied()
    {
        var service = Service();
        var id = await service.CreateAnnouncementAsync(Manager(), "Normal", "Read when convenient.",
            StaffAnnouncementPriority.Normal, null, clock.UtcNow, null);
        var version = (await service.GetStaffHubAsync(WaiterOne())).Announcements.Single().Version;
        await Assert.ThrowsAsync<DomainException>(() => service.AcknowledgeAsync(WaiterOne(), id, version));

        await using (var db = new RomsDbContext(options))
        {
            var waiter = await db.Users.FindAsync("waiter-1");
            waiter!.IsActive = false;
            await db.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<DomainException>(() => service.GetStaffHubAsync(WaiterOne()));
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private StaffCommunicationService Service() => new(new TestFactory(options), clock);
    private static ClaimsPrincipal WaiterOne() => Principal("waiter-1", "waiter-one");
    private static ClaimsPrincipal WaiterTwo() => Principal("waiter-2", "waiter-two");
    private static ClaimsPrincipal Manager() => Principal("manager-1", "manager-one");
    private static ClaimsPrincipal Admin() => Principal("admin-1", "admin-one");

    private static ClaimsPrincipal Principal(string id, string username) => new(new ClaimsIdentity(new[]
    {
        new Claim(ClaimTypes.NameIdentifier, id), new Claim(ClaimTypes.Name, username)
    }, "Gate2ETest"));

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
