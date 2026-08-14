using Microsoft.EntityFrameworkCore;
using Roms.Application;
using Roms.Domain;
using Roms.Infrastructure.Identity;
using Roms.Infrastructure.Services;

namespace Roms.IntegrationTests;

[Collection(MariaDbCollection.Name)]
public sealed class MariaDbAttendanceConcurrencyTests(MariaDbFixture fixture)
{
    [Fact]
    public async Task Concurrent_workers_commit_exactly_one_automatic_closure_and_audit()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var clock = new FixedClock();
        Guid recordId;
        await using (var db = database.CreateContext())
        {
            var waiter = new ApplicationUser
            {
                UserName = "attendance-race-waiter",
                NormalizedUserName = "ATTENDANCE-RACE-WAITER",
                DisplayName = "Attendance Race Waiter"
            };
            var openRecord = AttendanceRecord.ClockIn(waiter.Id, null, clock.UtcNow.AddHours(-13));
            recordId = openRecord.Id;
            db.Users.Add(waiter);
            db.AttendanceRecords.Add(openRecord);
            await db.SaveChangesAsync();
        }

        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var attempts = new[]
        {
            Run(start.Task, new AttendanceAutoClosureService(database.CreateFactory(), clock)),
            Run(start.Task, new AttendanceAutoClosureService(database.CreateFactory(), clock))
        };
        start.SetResult();
        var results = await Task.WhenAll(attempts);

        Assert.Equal(1, results.Sum(x => x.Closed));
        await using var verify = database.CreateContext();
        var savedRecord = await verify.AttendanceRecords.SingleAsync(x => x.Id == recordId);
        Assert.Equal(clock.UtcNow.AddHours(-1), savedRecord.ClockOutUtc);
        Assert.True(savedRecord.RequiresManagerReview);
        Assert.Equal(1, await verify.AuditEntries.CountAsync(x =>
            x.EntityId == recordId.ToString() && x.Action == "AutomaticAttendanceClosure"));
    }

    [Fact]
    public async Task Concurrent_manual_and_automatic_clock_out_commit_exactly_one_closure()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var clock = new FixedClock();
        Guid recordId;
        await using (var db = database.CreateContext())
        {
            var waiter = new ApplicationUser
            {
                UserName = "manual-auto-race-waiter",
                NormalizedUserName = "MANUAL-AUTO-RACE-WAITER",
                DisplayName = "Manual Auto Race Waiter"
            };
            var openRecord = AttendanceRecord.ClockIn(waiter.Id, null, clock.UtcNow.AddHours(-13));
            recordId = openRecord.Id;
            db.Users.Add(waiter);
            db.AttendanceRecords.Add(openRecord);
            await db.SaveChangesAsync();
        }

        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var automatic = Run(start.Task, new AttendanceAutoClosureService(database.CreateFactory(), clock));
        var manual = RunManual(start.Task, new AttendanceService(database.CreateFactory(), clock), "manual-auto-race-waiter");
        start.SetResult();
        await Task.WhenAll(automatic, manual);

        await using var verify = database.CreateContext();
        var savedRecord = await verify.AttendanceRecords.SingleAsync(x => x.Id == recordId);
        Assert.NotNull(savedRecord.ClockOutUtc);
        Assert.True(savedRecord.ClosureKind is AttendanceClosureKind.Manual or
            AttendanceClosureKind.AutomaticUnscheduledLimit);
        Assert.Equal(1, await verify.AuditEntries.CountAsync(x => x.EntityId == recordId.ToString() &&
            (x.Action == "ClockOut" || x.Action == "AutomaticAttendanceClosure")));
    }

    private static async Task<AttendanceAutoClosureResult> Run(Task start, AttendanceAutoClosureService service)
    {
        await start;
        return await service.ProcessDueAsync();
    }

    private static async Task<Exception?> RunManual(Task start, AttendanceService service, string username)
    {
        await start;
        try
        {
            await service.ClockOutAsync(username);
            return null;
        }
        catch (DomainException exception)
        {
            return exception;
        }
    }

    private sealed class FixedClock : IClock
    {
        public DateTime UtcNow => new(2026, 8, 14, 12, 0, 0, DateTimeKind.Utc);
    }
}
