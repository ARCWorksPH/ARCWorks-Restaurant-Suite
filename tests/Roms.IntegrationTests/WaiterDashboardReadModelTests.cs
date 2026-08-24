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

public sealed class WaiterDashboardReadModelTests : IAsyncLifetime
{
    private DbContextOptions<RomsDbContext> options = default!;
    private readonly FixedRestaurantClock clock = new(
        new DateTime(2026, 8, 16, 16, 30, 0, DateTimeKind.Utc));

    public async Task InitializeAsync()
    {
        options = new DbContextOptionsBuilder<RomsDbContext>()
            .UseInMemoryDatabase($"roms-waiter-dashboard-{Guid.NewGuid():N}")
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        await using var db = new RomsDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var waiterRole = Role(RomsRoles.Waiter);
        var managerRole = Role(RomsRoles.Manager);
        db.Roles.AddRange(waiterRole, managerRole);

        var waiter = User("waiter-id", "waiter", "Waiter Two");
        var otherWaiter = User("other-id", "other", "Other Waiter");
        var inactiveWaiter = User("inactive-id", "inactive", "Inactive Waiter", false);
        var manager = User("manager-id", "manager", "Manager", portrait: "/images/staff/demo/team-01.svg");
        var teamMate = User("team-id", "team", "Team Mate", portrait: "/images/staff/demo/team-02.svg");
        var fallbackMate = User("fallback-id", "fallback", "Fallback Mate", portrait: "https://example.invalid/avatar.svg");
        var draftMate = User("draft-id", "draft", "Draft Mate", portrait: "/images/staff/demo/team-03.svg");
        draftMate.ProfileLifecycle = StaffProfileLifecycle.Draft;
        var nextDayMate = User("next-day-id", "next-day", "Next Day Mate", portrait: "/images/staff/demo/team-04.svg");
        db.Users.AddRange(waiter, otherWaiter, inactiveWaiter, manager, teamMate, fallbackMate, draftMate, nextDayMate);
        db.UserRoles.AddRange(
            UserRole(waiter.Id, waiterRole.Id),
            UserRole(otherWaiter.Id, waiterRole.Id),
            UserRole(inactiveWaiter.Id, waiterRole.Id),
            UserRole(manager.Id, managerRole.Id),
            UserRole(teamMate.Id, waiterRole.Id),
            UserRole(fallbackMate.Id, waiterRole.Id),
            UserRole(draftMate.Id, waiterRole.Id),
            UserRole(nextDayMate.Id, waiterRole.Id));

        var todayShift = new StaffSchedule { UserId = waiter.Id, CreatedBy = "admin" };
        todayShift.SetSchedule(
            clock.ToUtc(new DateTime(2026, 8, 17, 17, 0, 0)),
            clock.ToUtc(new DateTime(2026, 8, 17, 23, 30, 0)),
            "Welcome the VIP table.",
            clock.UtcNow);
        db.StaffSchedules.Add(todayShift);
        foreach (var user in new[] { manager, teamMate, fallbackMate })
        {
            var schedule = new StaffSchedule { UserId = user.Id, CreatedBy = "admin" };
            schedule.SetSchedule(
                clock.ToUtc(new DateTime(2026, 8, 17, 10, 0, 0)),
                clock.ToUtc(new DateTime(2026, 8, 17, 18, 0, 0)),
                "Demo schedule.", clock.UtcNow);
            db.StaffSchedules.Add(schedule);
        }
        var draftSchedule = new StaffSchedule { UserId = draftMate.Id, CreatedBy = "admin" };
        draftSchedule.SetSchedule(
            clock.ToUtc(new DateTime(2026, 8, 17, 10, 0, 0)),
            clock.ToUtc(new DateTime(2026, 8, 17, 18, 0, 0)),
            "Draft profile must not appear.", clock.UtcNow);
        var nextDaySchedule = new StaffSchedule { UserId = nextDayMate.Id, CreatedBy = "admin" };
        nextDaySchedule.SetSchedule(
            clock.ToUtc(new DateTime(2026, 8, 18, 0, 0, 0)),
            clock.ToUtc(new DateTime(2026, 8, 18, 8, 0, 0)),
            "Tomorrow only.", clock.UtcNow);
        db.StaffSchedules.AddRange(draftSchedule, nextDaySchedule);

        var mondayOpen = AttendanceRecord.ClockIn(
            waiter.Id,
            todayShift.Id,
            new DateTime(2026, 8, 16, 15, 30, 0, DateTimeKind.Utc));
        var sundayClosed = AttendanceRecord.ClockIn(
            waiter.Id,
            null,
            new DateTime(2026, 8, 15, 10, 0, 0, DateTimeKind.Utc));
        sundayClosed.ClockOut(new DateTime(2026, 8, 15, 18, 0, 0, DateTimeKind.Utc));
        db.AttendanceRecords.AddRange(mondayOpen, sundayClosed);

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Read_model_uses_principal_identity_and_returns_only_frozen_waiter_fields()
    {
        var view = await Service().GetAsync(Principal("waiter-id", "waiter"));

        Assert.Equal("Waiter Two", view.DisplayName);
        Assert.Equal("/images/staff/neutral-avatar.svg", view.PortraitPath);
        Assert.Equal(new DateOnly(2026, 8, 17), view.RestaurantDate);
        Assert.Equal(new DateTime(2026, 8, 17, 0, 30, 0), view.RestaurantNowLocal);
        Assert.True(view.IsClockedIn);
        Assert.True(view.CanEnterFloor);
        Assert.Equal(new DateTime(2026, 8, 16, 23, 30, 0), view.ClockInLocal);
        Assert.Equal(0.5m, view.HoursThisWeek);
        Assert.NotNull(view.TodayShift);
        Assert.Equal(new DateTime(2026, 8, 17, 17, 0, 0), view.TodayShift!.ScheduledStartLocal);
        Assert.Equal("Welcome the VIP table.", view.TodayShift.ManagerNote);
        Assert.Equal(2, view.RecentAttendance.Count);

        var fields = typeof(WaiterDashboardView).GetProperties().Select(x => x.Name).Order().ToArray();
        Assert.Equal(new[]
        {
            "AttendanceReviewNotice", "CanEnterFloor", "ClockInLocal", "DisplayName", "HoursThisWeek", "IsClockedIn",
            "PortraitPath", "RecentAttendance", "RestaurantDate", "RestaurantNowLocal", "TodayShift", "TodayTeam"
        }, fields);
        Assert.Equal(4, view.TodayTeam.Count);
        Assert.All(view.TodayTeam, item => Assert.DoesNotContain("Name", item.GetType().GetProperties().Select(x => x.Name)));
    }

    [Fact]
    public async Task Browser_supplied_name_cannot_redirect_the_principal_to_another_employee()
    {
        var principal = Principal("waiter-id", "other");

        var view = await Service().GetAsync(principal);

        Assert.Equal("Waiter Two", view.DisplayName);
        Assert.Equal(2, view.RecentAttendance.Count);
    }

    [Theory]
    [InlineData("manager-id", "manager")]
    [InlineData("inactive-id", "inactive")]
    public async Task Non_waiter_and_inactive_accounts_are_rejected(string userId, string username)
    {
        await Assert.ThrowsAsync<DomainException>(() => Service().GetAsync(Principal(userId, username)));
    }

    [Fact]
    public async Task Missing_authenticated_identity_is_rejected()
    {
        await Assert.ThrowsAsync<DomainException>(() => Service().GetAsync(new ClaimsPrincipal()));

        var untrustedClaims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "waiter-id")
        }));
        await Assert.ThrowsAsync<DomainException>(() => Service().GetAsync(untrustedClaims));
    }

    [Fact]
    public async Task Active_waiter_without_schedule_or_attendance_gets_safe_empty_state()
    {
        var view = await Service().GetAsync(Principal("other-id", "other"));

        Assert.Equal("Other Waiter", view.DisplayName);
        Assert.False(view.IsClockedIn);
        Assert.False(view.CanEnterFloor);
        Assert.Null(view.ClockInLocal);
        Assert.Null(view.TodayShift);
        Assert.Equal(0m, view.HoursThisWeek);
        Assert.Empty(view.RecentAttendance);
        Assert.Equal(4, view.TodayTeam.Count);
    }

    [Fact]
    public async Task Today_team_returns_only_local_approved_portraits_and_uses_neutral_fallback()
    {
        var view = await Service().GetAsync(Principal("waiter-id", "waiter"));

        Assert.Contains(view.TodayTeam, item => item.PortraitPath == "/images/staff/demo/team-01.svg" && !item.UsesFallback);
        Assert.Contains(view.TodayTeam, item => item.PortraitPath == "/images/staff/neutral-avatar.svg" && item.UsesFallback);
        Assert.Equal(4, view.TodayTeam.Count);
        Assert.All(view.TodayTeam, item => Assert.StartsWith("/images/staff/", item.PortraitPath));
        Assert.DoesNotContain(view.TodayTeam, item => item.PortraitPath == "/images/staff/demo/team-03.svg");
        Assert.DoesNotContain(view.TodayTeam, item => item.PortraitPath == "/images/staff/demo/team-04.svg");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private WaiterDashboardService Service() => new(new TestFactory(options), clock);

    private static ClaimsPrincipal Principal(string userId, string username) => new(
        new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, username)
        }, "Gate2BTest"));

    private static ApplicationUser User(string id, string username, string displayName, bool active = true, string? portrait = null) => new()
    {
        Id = id,
        UserName = username,
        NormalizedUserName = username.ToUpperInvariant(),
        DisplayName = displayName,
        IsActive = active,
        ProfilePortraitPath = portrait
    };

    private static IdentityRole Role(string name) => new(name)
    {
        Id = $"{name.ToLowerInvariant()}-role-id",
        NormalizedName = name.ToUpperInvariant()
    };

    private static IdentityUserRole<string> UserRole(string userId, string roleId) => new()
    { UserId = userId, RoleId = roleId };

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
