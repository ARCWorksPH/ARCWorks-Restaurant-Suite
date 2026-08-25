using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Roms.Application;
using Roms.Domain;
using Roms.Infrastructure.Persistence;

namespace Roms.Infrastructure.Services;

public sealed class WaiterDashboardService(
    IDbContextFactory<RomsDbContext> factory,
    IRestaurantClock restaurantClock) : IWaiterDashboardService
{
    private const int RecentRecordLimit = 3;
    private const string DefaultPortraitPath = "/images/staff/neutral-avatar.svg";

    public async Task<WaiterDashboardView> GetAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(principal);
        if (principal.Identity?.IsAuthenticated != true)
            throw new DomainException("An authenticated staff identity is required.");
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("An authenticated staff identity is required.");

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var waiter = await (from user in db.Users.AsNoTracking()
                            join userRole in db.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
                            join role in db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                            where user.Id == userId && user.IsActive && role.Name == RomsRoles.Waiter
                            select new { user.Id, user.DisplayName, user.UserName, user.ProfilePortraitPath })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new DomainException("An active Waiter account is required.");

        var localDate = restaurantClock.LocalDate;
        var dayStartUtc = restaurantClock.ToUtc(localDate.ToDateTime(TimeOnly.MinValue));
        var dayEndUtc = restaurantClock.ToUtc(localDate.AddDays(1).ToDateTime(TimeOnly.MinValue));
        var weekStart = restaurantClock.StartOfWeek(localDate);
        var weekStartUtc = restaurantClock.ToUtc(weekStart.ToDateTime(TimeOnly.MinValue));
        var nowUtc = restaurantClock.UtcNow;

        var openAttendance = await db.AttendanceRecords.AsNoTracking()
            .Where(x => x.UserId == waiter.Id && x.ClockOutUtc == null)
            .OrderByDescending(x => x.ClockInUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var todaySchedule = await db.StaffSchedules.AsNoTracking()
            .Where(x => x.UserId == waiter.Id &&
                        x.ScheduledStartUtc < dayEndUtc && x.ScheduledEndUtc > dayStartUtc)
            .OrderBy(x => x.ScheduledStartUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var weeklyRecords = await db.AttendanceRecords.AsNoTracking()
            .Where(x => x.UserId == waiter.Id &&
                        x.ClockInUtc < nowUtc && (x.ClockOutUtc == null || x.ClockOutUtc > weekStartUtc))
            .OrderByDescending(x => x.ClockInUtc)
            .ToListAsync(cancellationToken);

        var recentRecords = await db.AttendanceRecords.AsNoTracking()
            .Where(x => x.UserId == waiter.Id && x.ClockInUtc < nowUtc)
            .OrderByDescending(x => x.ClockInUtc)
            .Take(RecentRecordLimit)
            .ToListAsync(cancellationToken);

        var pendingReview = await db.AttendanceRecords.AsNoTracking()
            .Where(x => x.UserId == waiter.Id && x.RequiresManagerReview && x.ClockOutUtc != null && x.ClosureKind != null)
            .OrderByDescending(x => x.ClockOutUtc)
            .FirstOrDefaultAsync(cancellationToken);

        // Membership deliberately uses the restaurant's calendar date only. It
        // does not expose roster times or make the carousel depend on a shift
        // being active at this exact moment.
        var todayTeam = await (from schedule in db.StaffSchedules.AsNoTracking()
                               join user in db.Users.AsNoTracking() on schedule.UserId equals user.Id
                               where user.IsActive && user.ProfileLifecycle == StaffProfileLifecycle.Approved &&
                                     schedule.ScheduledStartUtc < dayEndUtc && schedule.ScheduledEndUtc > dayStartUtc
                               select new { user.Id, user.ProfilePortraitPath })
            .Distinct()
            .OrderBy(member => member.Id)
            .ToListAsync(cancellationToken);

        var weeklyHours = weeklyRecords.Sum(record => DurationHours(
            record.ClockInUtc < weekStartUtc ? weekStartUtc : record.ClockInUtc,
            (record.ClockOutUtc ?? nowUtc) > nowUtc ? nowUtc : record.ClockOutUtc ?? nowUtc));

        return new WaiterDashboardView(
            DisplayName: string.IsNullOrWhiteSpace(waiter.DisplayName) ? waiter.UserName! : waiter.DisplayName,
            PortraitPath: IsLocalPortrait(waiter.ProfilePortraitPath)
                ? waiter.ProfilePortraitPath!
                : DefaultPortraitPath,
            RestaurantNowLocal: restaurantClock.LocalNow,
            RestaurantDate: localDate,
            IsClockedIn: openAttendance is not null,
            ClockInLocal: openAttendance is null ? null : restaurantClock.ToLocal(openAttendance.ClockInUtc),
            CanEnterFloor: openAttendance is not null,
            TodayShift: todaySchedule is null ? null : new WaiterShiftSummary(
                restaurantClock.ToLocal(todaySchedule.ScheduledStartUtc),
                restaurantClock.ToLocal(todaySchedule.ScheduledEndUtc),
                todaySchedule.Notes),
            HoursThisWeek: weeklyHours,
            RecentAttendance: recentRecords.Select(record => new WaiterAttendanceSummary(
                restaurantClock.ToLocal(record.ClockInUtc),
                record.ClockOutUtc is null ? null : restaurantClock.ToLocal(record.ClockOutUtc.Value),
                DurationHours(record.ClockInUtc, record.ClockOutUtc ?? nowUtc))).ToList(),
            AttendanceReviewNotice: pendingReview is null ? null : new AttendanceReviewNotice(
                restaurantClock.ToLocal(pendingReview.ClockOutUtc!.Value), pendingReview.ClosureKind!.Value),
            TodayTeam: todayTeam.Select(member => new TodayTeamPortrait(
                IsLocalPortrait(member.ProfilePortraitPath) ? member.ProfilePortraitPath! : DefaultPortraitPath,
                !IsLocalPortrait(member.ProfilePortraitPath))).ToList());
    }

    private static bool IsLocalPortrait(string? path) =>
        !string.IsNullOrWhiteSpace(path) &&
        path.StartsWith("/images/staff/", StringComparison.Ordinal) &&
        path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase);

    private static decimal DurationHours(DateTime startUtc, DateTime endUtc) =>
        endUtc <= startUtc ? 0m : Math.Round((decimal)(endUtc - startUtc).TotalHours, 2);
}
