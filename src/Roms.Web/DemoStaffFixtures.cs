using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Roms.Application;
using Roms.Domain;
using Roms.Infrastructure.Identity;
using Roms.Infrastructure.Persistence;

namespace Roms.Web;

/// <summary>
/// Development/demo-only staff visual fixtures. These accounts intentionally
/// have no password and cannot be used to sign in. They make the dashboard's
/// team, schedule, attendance, and portrait states testable without pretending
/// to be a restaurant's real personnel data.
/// </summary>
public static class DemoStaffFixtures
{
    private sealed record Definition(string UserName, string DisplayName, string Role, string Portrait);

    private static readonly Definition[] Definitions =
    [
        new("demo-waiter-01", "Demo Waiter One", RomsRoles.Waiter, "/images/staff/demo/team-01.svg"),
        new("demo-waiter-02", "Demo Waiter Two", RomsRoles.Waiter, "/images/staff/demo/team-02.svg"),
        new("demo-waiter-03", "Demo Waiter Three", RomsRoles.Waiter, "/images/staff/demo/team-03.svg"),
        new("demo-kitchen-01", "Demo Kitchen One", RomsRoles.Kitchen, "/images/staff/demo/team-04.svg"),
        new("demo-kitchen-02", "Demo Kitchen Two", RomsRoles.Kitchen, "/images/staff/demo/team-05.svg"),
        new("demo-kitchen-03", "Demo Kitchen Three", RomsRoles.Kitchen, "/images/staff/demo/team-06.svg"),
        new("demo-manager-01", "Demo Manager One", RomsRoles.Manager, "/images/staff/demo/team-07.svg"),
        new("demo-manager-02", "Demo Manager Two", RomsRoles.Manager, "/images/staff/demo/team-08.svg"),
        new("demo-support-01", "Demo Support One", RomsRoles.Waiter, "/images/staff/demo/team-09.svg"),
        new("demo-support-02", "Demo Support Two", RomsRoles.Kitchen, "/images/staff/demo/team-10.svg")
    ];

    public static async Task EnsureAsync(
        RomsDbContext db,
        UserManager<ApplicationUser> users,
        IRestaurantClock clock,
        CancellationToken cancellationToken = default)
    {
        var localDate = clock.LocalDate;

        foreach (var definition in Definitions)
        {
            var user = await users.FindByNameAsync(definition.UserName);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = definition.UserName,
                    DisplayName = definition.DisplayName,
                    EmailConfirmed = true,
                    IsActive = true,
                    IsDemoProfile = true,
                    ProfilePortraitPath = definition.Portrait,
                    ProfileLifecycle = StaffProfileLifecycle.Approved,
                    ProfileUpdatedUtc = clock.UtcNow
                };
                var created = await users.CreateAsync(user);
                if (!created.Succeeded)
                    throw new InvalidOperationException(string.Join("; ", created.Errors.Select(error => error.Description)));

                db.AuditEntries.Add(new AuditEntry
                {
                    ActorId = "demo-fixtures",
                    Action = "SeedDemoStaffProfile",
                    EntityType = nameof(ApplicationUser),
                    EntityId = user.Id,
                    NewValuesJson = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        user.DisplayName,
                        user.ProfilePortraitPath,
                        user.ProfileLifecycle,
                        user.IsDemoProfile
                    }),
                    Reason = "Development/demo fixture only; replace before restaurant use.",
                    OccurredUtc = clock.UtcNow
                });
            }

            if (!await users.IsInRoleAsync(user, definition.Role))
                await users.AddToRoleAsync(user, definition.Role);

            foreach (var date in Enumerable.Range(-3, 7).Select(localDate.AddDays))
            {
                var startLocal = date.ToDateTime(new TimeOnly(10, 0));
                var endLocal = date.ToDateTime(new TimeOnly(18, 0));
                var startUtc = clock.ToUtc(startLocal);
                var endUtc = clock.ToUtc(endLocal);
                var schedule = await db.StaffSchedules.SingleOrDefaultAsync(
                    x => x.UserId == user.Id && x.ScheduledStartUtc == startUtc,
                    cancellationToken);

                if (schedule is null)
                {
                    schedule = new StaffSchedule { UserId = user.Id, CreatedBy = "demo-fixtures" };
                    schedule.SetSchedule(
                        startUtc,
                        endUtc,
                        date == localDate
                            ? "Demo current-day shift fixture — replace before restaurant use."
                            : "Demo schedule fixture — replace before restaurant use.",
                        clock.UtcNow);
                    db.StaffSchedules.Add(schedule);

                    db.AuditEntries.Add(new AuditEntry
                    {
                        ActorId = "demo-fixtures",
                        Action = "SeedDemoStaffSchedule",
                        EntityType = nameof(StaffSchedule),
                        EntityId = schedule.Id.ToString("N"),
                        NewValuesJson = System.Text.Json.JsonSerializer.Serialize(new { user.Id, StartUtc = startUtc, EndUtc = endUtc }),
                        Reason = "Development/demo fixture only; replace before restaurant use.",
                        OccurredUtc = clock.UtcNow
                    });
                }

                if (date >= localDate)
                    continue;

                var clockInUtc = startUtc.AddMinutes(5);
                if (!await db.AttendanceRecords.AnyAsync(
                        x => x.UserId == user.Id && x.ClockInUtc == clockInUtc,
                        cancellationToken))
                {
                    var attendance = AttendanceRecord.ClockIn(user.Id, schedule.Id, clockInUtc);
                    attendance.ClockOut(endUtc.AddMinutes(-10));
                    db.AttendanceRecords.Add(attendance);
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
