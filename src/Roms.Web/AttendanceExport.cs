using System.Text;
using Microsoft.AspNetCore.Authorization;
using Roms.Application;

namespace Roms.Web;

public static class AttendanceExport
{
    public static IEndpointRouteBuilder MapAttendanceExport(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/admin/attendance/export.csv", async (DateOnly? from, System.Security.Claims.ClaimsPrincipal user, IAttendanceService attendance, CancellationToken ct) =>
        {
            var adminId = user.Identity?.Name ?? "unknown";
            var zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
            var startLocal = (from ?? DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone))).ToDateTime(TimeOnly.MinValue);
            var startUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(startLocal, DateTimeKind.Unspecified), zone);
            var endUtc = startUtc.AddDays(7);
            var view = await attendance.GetAdminViewAsync(adminId, startUtc, endUtc, ct);
            var csv = new StringBuilder("Staff,Username,Clock In,Clock Out,Hours,Correction Reason\r\n");
            foreach (var item in view.Records.OrderBy(x => x.DisplayName).ThenBy(x => x.ClockInUtc))
            {
                var clockIn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(item.ClockInUtc, DateTimeKind.Utc), zone);
                var clockOut = item.ClockOutUtc is null ? "" : TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(item.ClockOutUtc.Value, DateTimeKind.Utc), zone).ToString("yyyy-MM-dd HH:mm");
                csv.AppendLine(string.Join(',', Escape(item.DisplayName), Escape(item.Username), Escape(clockIn.ToString("yyyy-MM-dd HH:mm")),
                    Escape(clockOut), item.Hours.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), Escape(item.AdjustmentReason ?? "")));
            }
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
            return Results.File(bytes, "text/csv", $"roms-attendance-{startLocal:yyyy-MM-dd}.csv");
        }).RequireAuthorization(new AuthorizeAttribute { Roles = RomsRoles.Admin });

        endpoints.MapGet("/admin/attendance/schedule-export.csv", async (DateOnly? from, System.Security.Claims.ClaimsPrincipal user, IAttendanceService attendance, CancellationToken ct) =>
        {
            var adminId = user.Identity?.Name ?? "unknown";
            var zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
            var startLocal = (from ?? DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone))).ToDateTime(TimeOnly.MinValue);
            var startUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(startLocal, DateTimeKind.Unspecified), zone);
            var view = await attendance.GetAdminViewAsync(adminId, startUtc, startUtc.AddDays(7), ct);
            var csv = new StringBuilder("Staff,Username,Start,End,Notes\r\n");
            foreach (var item in view.Schedules.OrderBy(x => x.ScheduledStartUtc).ThenBy(x => x.DisplayName))
            {
                var start = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(item.ScheduledStartUtc, DateTimeKind.Utc), zone);
                var end = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(item.ScheduledEndUtc, DateTimeKind.Utc), zone);
                csv.AppendLine(string.Join(',', Escape(item.DisplayName), Escape(item.Username),
                    Escape(start.ToString("yyyy-MM-dd HH:mm")), Escape(end.ToString("yyyy-MM-dd HH:mm")), Escape(item.Notes)));
            }
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
            return Results.File(bytes, "text/csv", $"roms-schedule-{startLocal:yyyy-MM-dd}.csv");
        }).RequireAuthorization(new AuthorizeAttribute { Roles = RomsRoles.Admin });

        endpoints.MapGet("/admin/attendance/schedule-template.csv", (System.Security.Claims.ClaimsPrincipal user) =>
        {
            var csv = "Staff,Username,Start,End,Notes\r\n" +
                      "Example Staff,username,2026-08-17 09:00,2026-08-17 17:00,Optional shift note\r\n";
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
            return Results.File(bytes, "text/csv", "roms-schedule-template.csv");
        }).RequireAuthorization(new AuthorizeAttribute { Roles = RomsRoles.Admin });

        return endpoints;
    }

    private static string Escape(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
}
