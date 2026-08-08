using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Roms.Application;
using Roms.Domain;
using Roms.Infrastructure.Persistence;

namespace Roms.Infrastructure.Services;

public sealed class AttendanceService(IDbContextFactory<RomsDbContext> factory, IClock clock) : IAttendanceService
{
    public async Task<MyAttendanceView> GetMineAsync(string username, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        ValidateRange(fromUtc, toUtc);
        await using var db = await factory.CreateDbContextAsync(ct);
        var user = await ActiveUserAsync(db, username, ct);
        var schedules = await db.StaffSchedules.AsNoTracking()
            .Where(x => x.UserId == user.Id && x.ScheduledEndUtc >= fromUtc && x.ScheduledStartUtc < toUtc)
            .OrderBy(x => x.ScheduledStartUtc).ToListAsync(ct);
        var records = await db.AttendanceRecords.AsNoTracking()
            .Where(x => x.UserId == user.Id && x.ClockInUtc >= fromUtc && x.ClockInUtc < toUtc)
            .OrderByDescending(x => x.ClockInUtc).ToListAsync(ct);
        var open = await db.AttendanceRecords.AsNoTracking().Where(x => x.UserId == user.Id && x.ClockOutUtc == null)
            .OrderByDescending(x => x.ClockInUtc).FirstOrDefaultAsync(ct);
        return new(open is null ? null : Map(open, user.UserName!, user.DisplayName),
            schedules.Select(x => Map(x, user.UserName!, user.DisplayName)).ToList(),
            records.Select(x => Map(x, user.UserName!, user.DisplayName)).ToList());
    }

    public async Task ClockInAsync(string username, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var user = await ActiveUserAsync(db, username, ct);
        if (await db.AttendanceRecords.AnyAsync(x => x.UserId == user.Id && x.ClockOutUtc == null, ct))
            throw new DomainException("You are already clocked in.");

        var now = clock.UtcNow;
        var schedule = await db.StaffSchedules.Where(x => x.UserId == user.Id &&
                x.ScheduledStartUtc <= now.AddHours(4) && x.ScheduledEndUtc >= now.AddHours(-12))
            .OrderBy(x => x.ScheduledStartUtc).FirstOrDefaultAsync(ct);
        var record = AttendanceRecord.ClockIn(user.Id, schedule?.Id, now);
        db.AttendanceRecords.Add(record);
        db.AuditEntries.Add(Audit(username, "ClockIn", record.Id, null, JsonSerializer.Serialize(new { record.ClockInUtc, record.StaffScheduleId })));
        await db.SaveChangesAsync(ct);
    }

    public async Task ClockOutAsync(string username, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var user = await ActiveUserAsync(db, username, ct);
        var record = await db.AttendanceRecords.Where(x => x.UserId == user.Id && x.ClockOutUtc == null)
            .OrderByDescending(x => x.ClockInUtc).FirstOrDefaultAsync(ct)
            ?? throw new DomainException("You are not currently clocked in.");
        record.ClockOut(clock.UtcNow);
        db.AuditEntries.Add(Audit(username, "ClockOut", record.Id, null, JsonSerializer.Serialize(new { record.ClockOutUtc })));
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<StaffMemberView>> GetStaffAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Users.AsNoTracking().Where(x => x.IsActive && x.UserName != null)
            .OrderBy(x => x.DisplayName).Select(x => new StaffMemberView(x.Id, x.UserName!, x.DisplayName)).ToListAsync(ct);
    }

    public async Task<AttendanceAdminView> GetAdminViewAsync(string adminId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        ValidateRange(fromUtc, toUtc);
        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureAdminAsync(db, adminId, ct);
        var users = await db.Users.AsNoTracking().Where(x => x.UserName != null)
            .Select(x => new StaffIdentity(x.Id, x.UserName!, x.DisplayName)).ToDictionaryAsync(x => x.Id, ct);
        var activeUserIds = await db.Users.AsNoTracking()
            .Where(x => x.IsActive)
            .Select(x => x.Id)
            .ToListAsync(ct);
        var schedules = await db.StaffSchedules.AsNoTracking()
            .Where(x => activeUserIds.Contains(x.UserId) && x.ScheduledEndUtc >= fromUtc && x.ScheduledStartUtc < toUtc)
            .OrderBy(x => x.ScheduledStartUtc).ToListAsync(ct);
        var records = await db.AttendanceRecords.AsNoTracking()
            .Where(x => x.ClockInUtc >= fromUtc && x.ClockInUtc < toUtc)
            .OrderByDescending(x => x.ClockInUtc).ToListAsync(ct);
        var present = await db.AttendanceRecords.AsNoTracking().Where(x => x.ClockOutUtc == null)
            .OrderBy(x => x.ClockInUtc).ToListAsync(ct);
        return new(schedules.Select(x => Map(x, users)).ToList(), records.Select(x => Map(x, users)).ToList(), present.Select(x => Map(x, users)).ToList());
    }

    public async Task<ManagerOperationalView> GetManagerViewAsync(string actorId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureManagerOrAdminAsync(db, actorId, ct);
        var present = await (from record in db.AttendanceRecords.AsNoTracking()
                             join user in db.Users.AsNoTracking() on record.UserId equals user.Id
                             where record.ClockOutUtc == null && user.IsActive && user.UserName != null
                             orderby record.ClockInUtc
                             select new ManagerPresenceView(record.UserId, user.UserName!, user.DisplayName, record.ClockInUtc))
            .ToListAsync(ct);
        return new(present);
    }

    public async Task SaveScheduleAsync(Guid? scheduleId, string userId, DateTime startUtc, DateTime endUtc, string? notes, string adminId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureAdminAsync(db, adminId, ct);
        if (!await db.Users.AnyAsync(x => x.Id == userId && x.IsActive, ct)) throw new DomainException("Staff member not found.");
        var overlaps = db.StaffSchedules.Where(x => x.UserId == userId && x.ScheduledStartUtc < endUtc && startUtc < x.ScheduledEndUtc);
        if (scheduleId.HasValue) overlaps = overlaps.Where(x => x.Id != scheduleId.Value);
        if (await overlaps.AnyAsync(ct))
            throw new DomainException("This staff member already has an overlapping schedule.");
        var schedule = scheduleId is null ? new StaffSchedule { UserId = userId, CreatedBy = adminId, CreatedUtc = clock.UtcNow }
            : await db.StaffSchedules.SingleOrDefaultAsync(x => x.Id == scheduleId, ct) ?? throw new DomainException("Schedule not found.");
        if (scheduleId is not null && schedule.ScheduledStartUtc.Date < clock.UtcNow.Date)
            throw new DomainException("Only today's or future schedules can be edited.");
        var oldValues = scheduleId is null ? null : JsonSerializer.Serialize(new { schedule.ScheduledStartUtc, schedule.ScheduledEndUtc, schedule.Notes });
        schedule.SetSchedule(startUtc, endUtc, notes, clock.UtcNow);
        if (scheduleId is null) db.StaffSchedules.Add(schedule);
        db.AuditEntries.Add(Audit(adminId, scheduleId is null ? "CreateStaffSchedule" : "UpdateStaffSchedule", schedule.Id, oldValues,
            JsonSerializer.Serialize(new { schedule.UserId, schedule.ScheduledStartUtc, schedule.ScheduledEndUtc, schedule.Notes }), entityType: nameof(StaffSchedule)));
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteScheduleAsync(Guid scheduleId, string adminId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureAdminAsync(db, adminId, ct);
        var schedule = await db.StaffSchedules.SingleOrDefaultAsync(x => x.Id == scheduleId, ct) ?? throw new DomainException("Schedule not found.");
        db.StaffSchedules.Remove(schedule);
        db.AuditEntries.Add(Audit(adminId, "DeleteStaffSchedule", schedule.Id, JsonSerializer.Serialize(schedule), null, entityType: nameof(StaffSchedule)));
        await db.SaveChangesAsync(ct);
    }

    public async Task CorrectAsync(Guid attendanceId, DateTime clockInUtc, DateTime? clockOutUtc, string reason, string adminId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureAdminAsync(db, adminId, ct);
        var record = await db.AttendanceRecords.SingleOrDefaultAsync(x => x.Id == attendanceId, ct) ?? throw new DomainException("Attendance record not found.");
        if (clockOutUtc is null && await db.AttendanceRecords.AnyAsync(x => x.UserId == record.UserId && x.Id != record.Id && x.ClockOutUtc == null, ct))
            throw new DomainException("This staff member already has another open attendance record.");
        var oldValues = JsonSerializer.Serialize(new { record.ClockInUtc, record.ClockOutUtc });
        record.Correct(clockInUtc, clockOutUtc, adminId, reason, clock.UtcNow);
        db.AuditEntries.Add(Audit(adminId, "CorrectAttendance", record.Id, oldValues,
            JsonSerializer.Serialize(new { record.ClockInUtc, record.ClockOutUtc }), reason));
        await db.SaveChangesAsync(ct);
    }

    private static async Task<Identity.ApplicationUser> ActiveUserAsync(RomsDbContext db, string username, CancellationToken ct) =>
        await db.Users.SingleOrDefaultAsync(x => x.UserName == username && x.IsActive, ct) ?? throw new DomainException("Active staff account not found.");

    private static void ValidateRange(DateTime fromUtc, DateTime toUtc)
    {
        if (toUtc <= fromUtc) throw new DomainException("Date range end time must be after the start time.");
    }

    private AttendanceRecordView Map(AttendanceRecord x, string username, string displayName) => new(x.Id, x.UserId, username,
        string.IsNullOrWhiteSpace(displayName) ? username : displayName, x.StaffScheduleId, x.ClockInUtc, x.ClockOutUtc,
        (decimal)((x.ClockOutUtc ?? clock.UtcNow) - x.ClockInUtc).TotalHours, x.AdjustmentReason, x.AdjustedBy);

    private AttendanceRecordView Map(AttendanceRecord x, IReadOnlyDictionary<string, StaffIdentity> users)
    {
        var user = users[x.UserId];
        return Map(x, user.Username, user.DisplayName);
    }

    private static StaffScheduleView Map(StaffSchedule x, string username, string displayName) =>
        new(x.Id, x.UserId, username, string.IsNullOrWhiteSpace(displayName) ? username : displayName,
            x.ScheduledStartUtc, x.ScheduledEndUtc, x.Notes);

    private static StaffScheduleView Map(StaffSchedule x, IReadOnlyDictionary<string, StaffIdentity> users)
    {
        var user = users[x.UserId];
        return Map(x, user.Username, user.DisplayName);
    }

    private AuditEntry Audit(string actorId, string action, Guid id, string? oldValues, string? newValues, string? reason = null, string? entityType = null) => new()
        { ActorId = actorId, Action = action, EntityType = entityType ?? nameof(AttendanceRecord), EntityId = id.ToString(), OldValuesJson = oldValues,
            NewValuesJson = newValues, Reason = reason, OccurredUtc = clock.UtcNow };

    private static async Task EnsureAdminAsync(RomsDbContext db, string actorId, CancellationToken ct)
    {
        var allowed = await (from user in db.Users
                             join userRole in db.UserRoles on user.Id equals userRole.UserId
                             join role in db.Roles on userRole.RoleId equals role.Id
                             where user.UserName == actorId && role.Name == RomsRoles.Admin
                             select user.Id).AnyAsync(ct);
        if (!allowed) throw new DomainException("Only an administrator can perform this action.");
    }

    private static async Task EnsureManagerOrAdminAsync(RomsDbContext db, string actorId, CancellationToken ct)
    {
        var allowed = await (from user in db.Users
                             join userRole in db.UserRoles on user.Id equals userRole.UserId
                             join role in db.Roles on userRole.RoleId equals role.Id
                             where user.UserName == actorId && (role.Name == RomsRoles.Manager || role.Name == RomsRoles.Admin)
                             select user.Id).AnyAsync(ct);
        if (!allowed) throw new DomainException("Only a manager or administrator can perform this action.");
    }

    private sealed record StaffIdentity(string Id, string Username, string DisplayName);
}
