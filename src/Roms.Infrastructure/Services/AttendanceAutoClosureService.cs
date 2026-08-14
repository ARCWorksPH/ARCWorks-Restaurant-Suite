using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Roms.Application;
using Roms.Domain;
using Roms.Infrastructure.Persistence;

namespace Roms.Infrastructure.Services;

public sealed class AttendanceAutoClosureService(
    IDbContextFactory<RomsDbContext> factory,
    IClock clock) : IAttendanceAutoClosureService
{
    private static readonly TimeSpan MaximumOpenDuration = TimeSpan.FromHours(12);

    public async Task<AttendanceAutoClosureResult> ProcessDueAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        await using var scan = await factory.CreateDbContextAsync(cancellationToken);
        var candidates = await scan.AttendanceRecords.AsNoTracking()
            .Include(x => x.StaffSchedule)
            .Where(x => x.ClockOutUtc == null)
            .Select(x => new Candidate(
                x.Id,
                x.ClockInUtc,
                x.StaffSchedule == null ? null : x.StaffSchedule.ScheduledEndUtc))
            .ToListAsync(cancellationToken);

        var due = candidates
            .Select(ToDueCandidate)
            .Where(x => x.DueUtc <= now)
            .ToList();
        var closed = 0;
        var concurrencySkipped = 0;

        foreach (var candidate in due)
        {
            await using var db = await factory.CreateDbContextAsync(cancellationToken);

            if (db.Database.IsRelational())
            {
                var closureKind = candidate.UsesScheduledBoundary
                    ? AttendanceClosureKind.AutomaticScheduledLimit
                    : AttendanceClosureKind.AutomaticUnscheduledLimit;
                var strategy = db.Database.CreateExecutionStrategy();
                var claimed = await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
                    var affected = await db.AttendanceRecords
                        .Where(x => x.Id == candidate.Id && x.ClockOutUtc == null)
                        .ExecuteUpdateAsync(updates => updates
                            .SetProperty(x => x.ClockOutUtc, candidate.DueUtc)
                            .SetProperty(x => x.ClosureKind, closureKind)
                            .SetProperty(x => x.RequiresManagerReview, true)
                            .SetProperty(x => x.Version, x => x.Version + 1), cancellationToken);

                    if (affected != 1)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return false;
                    }

                    db.AuditEntries.Add(CreateAudit(candidate.Id, candidate.DueUtc, closureKind, now));
                    await db.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return true;
                });

                if (claimed) closed++;
                else concurrencySkipped++;
                continue;
            }

            var record = await db.AttendanceRecords.SingleOrDefaultAsync(x => x.Id == candidate.Id, cancellationToken);
            if (record is null || record.ClockOutUtc is not null)
            {
                concurrencySkipped++;
                continue;
            }

            record.CloseAutomatically(candidate.DueUtc, candidate.UsesScheduledBoundary);
            db.AuditEntries.Add(CreateAudit(record.Id, record.ClockOutUtc!.Value, record.ClosureKind!.Value, now));

            try
            {
                await db.SaveChangesAsync(cancellationToken);
                closed++;
            }
            catch (DbUpdateConcurrencyException)
            {
                concurrencySkipped++;
            }
        }

        return new AttendanceAutoClosureResult(candidates.Count, closed, concurrencySkipped);
    }

    private static AuditEntry CreateAudit(
        Guid recordId,
        DateTime clockOutUtc,
        AttendanceClosureKind closureKind,
        DateTime processedUtc) => new()
    {
        ActorId = "system:attendance-auto-closure",
        Action = "AutomaticAttendanceClosure",
        EntityType = nameof(AttendanceRecord),
        EntityId = recordId.ToString(),
        NewValuesJson = JsonSerializer.Serialize(new
        {
            ClockOutUtc = clockOutUtc,
            ClosureKind = closureKind,
            RequiresManagerReview = true,
            ProcessedUtc = processedUtc
        }),
        Reason = "Attendance exceeded the 12-hour automatic closure boundary.",
        OccurredUtc = processedUtc
    };

    private static DueCandidate ToDueCandidate(Candidate candidate)
    {
        var scheduledDueUtc = candidate.ScheduledEndUtc?.Add(MaximumOpenDuration);
        var usesScheduledBoundary = scheduledDueUtc is not null && scheduledDueUtc.Value > candidate.ClockInUtc;
        return new DueCandidate(
            candidate.Id,
            usesScheduledBoundary,
            usesScheduledBoundary ? scheduledDueUtc!.Value : candidate.ClockInUtc.Add(MaximumOpenDuration));
    }

    private sealed record Candidate(Guid Id, DateTime ClockInUtc, DateTime? ScheduledEndUtc);
    private sealed record DueCandidate(Guid Id, bool UsesScheduledBoundary, DateTime DueUtc);
}
