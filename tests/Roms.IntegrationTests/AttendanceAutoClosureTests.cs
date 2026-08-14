using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Roms.Application;
using Roms.Domain;
using Roms.Infrastructure.Identity;
using Roms.Infrastructure.Persistence;
using Roms.Infrastructure.Services;

namespace Roms.IntegrationTests;

public sealed class AttendanceAutoClosureTests
{
    [Fact]
    public async Task Scheduled_and_unscheduled_records_close_at_their_exact_boundaries_and_restart_is_idempotent()
    {
        var fixture = await Fixture.Create();
        var now = fixture.Clock.UtcNow;
        await using (var db = fixture.Context())
        {
            var schedule = new StaffSchedule { UserId = fixture.Waiter.Id, CreatedBy = "admin" };
            schedule.SetSchedule(now.AddHours(-20), now.AddHours(-13), null, now.AddHours(-20));
            var scheduled = AttendanceRecord.ClockIn(fixture.Waiter.Id, schedule.Id, now.AddHours(-20));
            var unscheduled = AttendanceRecord.ClockIn(fixture.OtherWaiter.Id, null, now.AddHours(-13));
            var recent = AttendanceRecord.ClockIn("recent-waiter", null, now.AddHours(-2));
            db.StaffSchedules.Add(schedule);
            db.AttendanceRecords.AddRange(scheduled, unscheduled, recent);
            await db.SaveChangesAsync();
        }

        var first = await fixture.AutoClosure.ProcessDueAsync();
        var afterRestart = await new AttendanceAutoClosureService(fixture.Factory, fixture.Clock).ProcessDueAsync();

        Assert.Equal(3, first.Examined);
        Assert.Equal(2, first.Closed);
        Assert.Equal(0, afterRestart.Closed);
        await using var verify = fixture.Context();
        var closed = await verify.AttendanceRecords.Where(x => x.ClockOutUtc != null).OrderBy(x => x.ClockInUtc).ToListAsync();
        Assert.Equal(2, closed.Count);
        Assert.All(closed, x => Assert.True(x.RequiresManagerReview));
        Assert.Contains(closed, x => x.ClosureKind == AttendanceClosureKind.AutomaticScheduledLimit && x.ClockOutUtc == now.AddHours(-1));
        Assert.Contains(closed, x => x.ClosureKind == AttendanceClosureKind.AutomaticUnscheduledLimit && x.ClockOutUtc == now.AddHours(-1));
        Assert.Equal(2, await verify.AuditEntries.CountAsync(x => x.Action == "AutomaticAttendanceClosure"));
    }

    [Fact]
    public async Task Manual_clock_out_wins_and_is_never_rewritten_by_the_worker()
    {
        var fixture = await Fixture.Create();
        Guid recordId;
        await using (var db = fixture.Context())
        {
            var record = AttendanceRecord.ClockIn(fixture.Waiter.Id, null, fixture.Clock.UtcNow.AddHours(-13));
            record.ClockOut(fixture.Clock.UtcNow.AddMinutes(-10));
            recordId = record.Id;
            db.AttendanceRecords.Add(record);
            await db.SaveChangesAsync();
        }

        var result = await fixture.AutoClosure.ProcessDueAsync();

        Assert.Equal(0, result.Closed);
        await using var verify = fixture.Context();
        var savedRecord = await verify.AttendanceRecords.SingleAsync(x => x.Id == recordId);
        Assert.Equal(AttendanceClosureKind.Manual, savedRecord.ClosureKind);
        Assert.False(savedRecord.RequiresManagerReview);
    }

    [Fact]
    public async Task Logout_or_idle_session_cleanup_does_not_clock_out_open_attendance()
    {
        var fixture = await Fixture.Create();
        Guid recordId;
        await using (var db = fixture.Context())
        {
            var user = await db.Users.SingleAsync(x => x.Id == fixture.Waiter.Id);
            user.ActiveSessionId = "session-before-logout";
            user.ActiveApplicationInstanceId = new string('A', 64);
            user.SessionLastActivityUtc = fixture.Clock.UtcNow;
            var record = AttendanceRecord.ClockIn(user.Id, null, fixture.Clock.UtcNow.AddHours(-11));
            recordId = record.Id;
            db.AttendanceRecords.Add(record);
            await db.SaveChangesAsync();

            // Both explicit logout and idle-session cleanup clear these three fields. Attendance
            // is intentionally independent and must remain open until clock-out or its 12-hour cap.
            user.ActiveSessionId = null;
            user.ActiveApplicationInstanceId = null;
            user.SessionLastActivityUtc = null;
            await db.SaveChangesAsync();
        }

        var result = await fixture.AutoClosure.ProcessDueAsync();

        Assert.Equal(0, result.Closed);
        await using var verify = fixture.Context();
        var savedRecord = await verify.AttendanceRecords.SingleAsync(x => x.Id == recordId);
        Assert.Null(savedRecord.ClockOutUtc);
    }

    [Fact]
    public async Task Expired_schedule_boundary_never_creates_a_clock_out_before_clock_in()
    {
        var fixture = await Fixture.Create();
        Guid recordId;
        await using (var db = fixture.Context())
        {
            var schedule = new StaffSchedule { UserId = fixture.Waiter.Id, CreatedBy = "admin" };
            schedule.SetSchedule(fixture.Clock.UtcNow.AddHours(-30), fixture.Clock.UtcNow.AddHours(-26), null,
                fixture.Clock.UtcNow.AddHours(-30));
            var record = AttendanceRecord.ClockIn(fixture.Waiter.Id, schedule.Id, fixture.Clock.UtcNow.AddHours(-13));
            recordId = record.Id;
            db.AddRange(schedule, record);
            await db.SaveChangesAsync();
        }

        await fixture.AutoClosure.ProcessDueAsync();

        await using var verify = fixture.Context();
        var savedRecord = await verify.AttendanceRecords.SingleAsync(x => x.Id == recordId);
        Assert.Equal(fixture.Clock.UtcNow.AddHours(-1), savedRecord.ClockOutUtc);
        Assert.Equal(AttendanceClosureKind.AutomaticUnscheduledLimit, savedRecord.ClosureKind);
    }

    [Fact]
    public async Task Manager_review_is_explicit_audited_and_does_not_change_recorded_hours()
    {
        var fixture = await Fixture.Create();
        Guid recordId;
        await using (var db = fixture.Context())
        {
            var record = AttendanceRecord.ClockIn(fixture.Waiter.Id, null, fixture.Clock.UtcNow.AddHours(-13));
            recordId = record.Id;
            db.AttendanceRecords.Add(record);
            await db.SaveChangesAsync();
        }
        await fixture.AutoClosure.ProcessDueAsync();
        var service = new AttendanceService(fixture.Factory, fixture.Clock);

        await service.ReviewAutomaticClosureAsync(recordId, "Confirmed with shift supervisor.", "manager");

        await using var verify = fixture.Context();
        var savedRecord = await verify.AttendanceRecords.SingleAsync(x => x.Id == recordId);
        Assert.False(savedRecord.RequiresManagerReview);
        Assert.Equal("manager", savedRecord.ReviewedBy);
        Assert.Equal(fixture.Clock.UtcNow.AddHours(-1), savedRecord.ClockOutUtc);
        Assert.True(await verify.AuditEntries.AnyAsync(x => x.Action == "ReviewAutomaticAttendanceClosure"));
    }

    [Fact]
    public async Task Waiter_floor_commands_require_open_attendance_and_are_revoked_after_auto_close()
    {
        var fixture = await Fixture.Create();
        Guid tableId;
        Guid menuItemId;
        await using (var db = fixture.Context())
        {
            var category = new MenuCategory { Name = "Mains" };
            var item = new MenuItem { Name = "Test Meal", Price = 100m, Category = category };
            var table = new RestaurantTable { Number = "1" };
            db.AddRange(category, item, table);
            await db.SaveChangesAsync();
            tableId = table.Id;
            menuItemId = item.Id;
        }
        var orders = new OrderService(fixture.Factory, fixture.Clock, new NoOpPublisher(), NullLogger<OrderService>.Instance);
        await Assert.ThrowsAsync<DomainException>(() => orders.GetOrCreateDraftAsync(tableId, "waiter"));
        await new AttendanceService(fixture.Factory, fixture.Clock).ClockInAsync("waiter");
        var orderId = await orders.GetOrCreateDraftAsync(tableId, "waiter");
        fixture.Clock.UtcNow = fixture.Clock.UtcNow.AddHours(13);
        await fixture.AutoClosure.ProcessDueAsync();

        await Assert.ThrowsAsync<DomainException>(() => orders.AddItemAsync(orderId, menuItemId, 1, null, "waiter"));
    }

    private sealed class Fixture
    {
        public MutableClock Clock { get; } = new() { UtcNow = new DateTime(2026, 8, 14, 12, 0, 0, DateTimeKind.Utc) };
        public ApplicationUser Waiter { get; private init; } = default!;
        public ApplicationUser OtherWaiter { get; private init; } = default!;
        public TestFactory Factory { get; private init; } = default!;
        public AttendanceAutoClosureService AutoClosure => new(Factory, Clock);
        public RomsDbContext Context() => Factory.CreateDbContext();

        public static async Task<Fixture> Create()
        {
            var options = new DbContextOptionsBuilder<RomsDbContext>()
                .UseInMemoryDatabase($"attendance-auto-close-{Guid.NewGuid():N}")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options;
            var factory = new TestFactory(options);
            var waiter = User("waiter-id", "waiter");
            var other = User("other-id", "other");
            await using var db = factory.CreateDbContext();
            await db.Database.EnsureCreatedAsync();
            var waiterRole = Role(RomsRoles.Waiter);
            var managerRole = Role(RomsRoles.Manager);
            db.Roles.AddRange(waiterRole, managerRole);
            db.Users.AddRange(waiter, other, User("manager-id", "manager"), User("recent-waiter", "recent"));
            db.UserRoles.AddRange(
                Link(waiter.Id, waiterRole.Id), Link(other.Id, waiterRole.Id),
                Link("recent-waiter", waiterRole.Id), Link("manager-id", managerRole.Id));
            await db.SaveChangesAsync();
            return new Fixture { Factory = factory, Waiter = waiter, OtherWaiter = other };
        }

        private static ApplicationUser User(string id, string username) => new()
        {
            Id = id, UserName = username, NormalizedUserName = username.ToUpperInvariant(), DisplayName = username, IsActive = true
        };
        private static IdentityRole Role(string name) => new(name) { Id = $"{name}-role", NormalizedName = name.ToUpperInvariant() };
        private static IdentityUserRole<string> Link(string userId, string roleId) => new() { UserId = userId, RoleId = roleId };
    }

    private sealed class MutableClock : IClock { public DateTime UtcNow { get; set; } }
    private sealed class TestFactory(DbContextOptions<RomsDbContext> options) : IDbContextFactory<RomsDbContext>
    {
        public RomsDbContext CreateDbContext() => new(options);
        public Task<RomsDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(CreateDbContext());
    }
    private sealed class NoOpPublisher : IOrderEventPublisher
    {
        public Task PublishAsync(OrderEvent message, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
