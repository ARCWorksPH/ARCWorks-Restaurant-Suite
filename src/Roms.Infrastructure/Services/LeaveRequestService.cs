using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Roms.Application;
using Roms.Domain;
using Roms.Infrastructure.Persistence;

namespace Roms.Infrastructure.Services;

public sealed class LeaveRequestService(
    IDbContextFactory<RomsDbContext> factory,
    IRestaurantClock restaurantClock) : ILeaveRequestService
{
    public async Task<IReadOnlyList<LeaveRequestView>> GetMineAsync(ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var employee = await RequireActiveEmployeeAsync(db, principal, cancellationToken);
        var requests = await db.LeaveRequests.AsNoTracking().Include(x => x.Dates)
            .Where(x => x.UserId == employee.Id)
            .OrderByDescending(x => x.SubmittedUtc)
            .ToListAsync(cancellationToken);
        return requests.Select(x => Map(x, employee.DisplayName)).ToList();
    }

    public async Task<IReadOnlyList<LeaveRequestView>> GetForDecisionAsync(ClaimsPrincipal actor,
        LeaveRequestStatus? status = LeaveRequestStatus.Pending, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await RequireManagerOrAdminAsync(db, actor, cancellationToken);
        var query = db.LeaveRequests.AsNoTracking().Include(x => x.Dates).AsQueryable();
        if (status is not null) query = query.Where(x => x.Status == status);
        var requests = await query.OrderBy(x => x.SubmittedUtc).ToListAsync(cancellationToken);
        var userIds = requests.Select(x => x.UserId).Distinct().ToList();
        var names = await db.Users.AsNoTracking().Where(x => userIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => string.IsNullOrWhiteSpace(x.DisplayName) ? x.UserName ?? x.Id : x.DisplayName,
                cancellationToken);
        return requests.Select(x => Map(x, names.GetValueOrDefault(x.UserId, x.UserId))).ToList();
    }

    public async Task<Guid> SubmitAsync(ClaimsPrincipal principal, IReadOnlyCollection<DateOnly> requestedDates,
        string? leaveType, string? privateMessage, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var employee = await RequireActiveEmployeeAsync(db, principal, cancellationToken);
        var request = new LeaveRequest { UserId = employee.Id, SubmittedUtc = restaurantClock.UtcNow };
        request.SetDetails(requestedDates, leaveType, privateMessage, restaurantClock.LocalDate, restaurantClock.UtcNow);
        await EnsureNoOverlapAsync(db, employee.Id, request.Dates.Select(x => x.RequestedDate), null, cancellationToken);
        db.LeaveRequests.Add(request);
        db.AuditEntries.Add(Audit(employee.UserName, "SubmitLeaveRequest", request.Id, null, Snapshot(request)));
        await SaveAsync(db, cancellationToken);
        return request.Id;
    }

    public async Task UpdateAsync(ClaimsPrincipal principal, Guid requestId, long expectedVersion,
        IReadOnlyCollection<DateOnly> requestedDates, string? leaveType, string? privateMessage,
        CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var employee = await RequireActiveEmployeeAsync(db, principal, cancellationToken);
        var request = await db.LeaveRequests.Include(x => x.Dates)
            .SingleOrDefaultAsync(x => x.Id == requestId && x.UserId == employee.Id, cancellationToken)
            ?? throw new DomainException("Leave request not found.");
        EnsureVersion(request, expectedVersion);
        var oldValues = Snapshot(request);
        request.SetDetails(requestedDates, leaveType, privateMessage, restaurantClock.LocalDate, restaurantClock.UtcNow,
            isEdit: true);
        await EnsureNoOverlapAsync(db, employee.Id, request.Dates.Select(x => x.RequestedDate), request.Id,
            cancellationToken);
        db.AuditEntries.Add(Audit(employee.UserName, "UpdateLeaveRequest", request.Id, oldValues, Snapshot(request)));
        await SaveAsync(db, cancellationToken);
    }

    public async Task CancelAsync(ClaimsPrincipal principal, Guid requestId, long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var employee = await RequireActiveEmployeeAsync(db, principal, cancellationToken);
        var request = await db.LeaveRequests.Include(x => x.Dates)
            .SingleOrDefaultAsync(x => x.Id == requestId && x.UserId == employee.Id, cancellationToken)
            ?? throw new DomainException("Leave request not found.");
        EnsureVersion(request, expectedVersion);
        var oldValues = Snapshot(request);
        request.Cancel(restaurantClock.LocalDate, restaurantClock.UtcNow);
        db.AuditEntries.Add(Audit(employee.UserName, "CancelLeaveRequest", request.Id, oldValues, Snapshot(request)));
        await SaveAsync(db, cancellationToken);
    }

    public async Task DecideAsync(ClaimsPrincipal actor, Guid requestId, long expectedVersion, bool approve,
        string? reason, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var reviewer = await RequireManagerOrAdminAsync(db, actor, cancellationToken);
        var request = await db.LeaveRequests.Include(x => x.Dates)
            .SingleOrDefaultAsync(x => x.Id == requestId, cancellationToken)
            ?? throw new DomainException("Leave request not found.");
        if (request.UserId == reviewer.Id) throw new DomainException("Staff cannot decide their own leave request.");
        EnsureVersion(request, expectedVersion);
        if (request.Dates.Any(x => x.RequestedDate <= restaurantClock.LocalDate))
            throw new DomainException("Leave requests cannot be decided after a requested date has arrived.");
        var oldValues = Snapshot(request);
        request.Decide(approve, reviewer.UserName, reason, restaurantClock.UtcNow);
        db.AuditEntries.Add(Audit(reviewer.UserName, approve ? "ApproveLeaveRequest" : "DeclineLeaveRequest",
            request.Id, oldValues, Snapshot(request), reason));
        await SaveAsync(db, cancellationToken);
    }

    private static async Task EnsureNoOverlapAsync(RomsDbContext db, string userId, IEnumerable<DateOnly> dates,
        Guid? excludingId, CancellationToken cancellationToken)
    {
        var requested = dates.Distinct().ToList();
        if (requested.Count == 0) return;
        var first = requested.Min();
        var last = requested.Max();
        // MySQL EF cannot type-map a parameterized DateOnly collection for SQL IN.
        // Keep the relational query bounded, then perform the exact comparison over
        // the at-most 31 requested dates in memory.
        var candidateDates = await db.LeaveRequestDates.AsNoTracking().Where(x =>
            x.RequestedDate >= first && x.RequestedDate <= last &&
            x.LeaveRequest.UserId == userId && x.LeaveRequest.Id != excludingId &&
            (x.LeaveRequest.Status == LeaveRequestStatus.Pending || x.LeaveRequest.Status == LeaveRequestStatus.Approved))
            .Select(x => x.RequestedDate)
            .ToListAsync(cancellationToken);
        if (candidateDates.Any(requested.Contains))
            throw new DomainException("A pending or approved leave request already covers one or more selected dates.");
    }

    private static void EnsureVersion(LeaveRequest request, long expectedVersion)
    {
        if (expectedVersion < 1 || request.Version != expectedVersion)
            throw new DomainException("The leave request changed since it was loaded. Refresh and try again.");
    }

    private static async Task<StaffActor> RequireActiveEmployeeAsync(RomsDbContext db, ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var id = RequireAuthenticatedId(principal);
        return await db.Users.AsNoTracking().Where(x => x.Id == id && x.IsActive && x.UserName != null)
            .Select(x => new StaffActor(x.Id, x.UserName!, string.IsNullOrWhiteSpace(x.DisplayName) ? x.UserName! : x.DisplayName))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new DomainException("An active staff account is required.");
    }

    private static async Task<StaffActor> RequireManagerOrAdminAsync(RomsDbContext db, ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var id = RequireAuthenticatedId(principal);
        return await (from user in db.Users
                      join userRole in db.UserRoles on user.Id equals userRole.UserId
                      join role in db.Roles on userRole.RoleId equals role.Id
                      where user.Id == id && user.IsActive && user.UserName != null &&
                            (role.Name == RomsRoles.Manager || role.Name == RomsRoles.Admin)
                      select new StaffActor(user.Id, user.UserName!,
                          string.IsNullOrWhiteSpace(user.DisplayName) ? user.UserName! : user.DisplayName))
            .Distinct().SingleOrDefaultAsync(cancellationToken)
            ?? throw new DomainException("Only a manager or administrator can decide leave requests.");
    }

    private static string RequireAuthenticatedId(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        if (principal.Identity?.IsAuthenticated != true) throw new DomainException("An authenticated staff identity is required.");
        return principal.FindFirstValue(ClaimTypes.NameIdentifier) is { Length: > 0 } id
            ? id
            : throw new DomainException("An authenticated staff identity is required.");
    }

    private async Task SaveAsync(RomsDbContext db, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new DomainException("The leave request changed since it was loaded. Refresh and try again.");
        }
    }

    private LeaveRequestView Map(LeaveRequest request, string displayName) => new(
        request.Id, request.UserId, displayName, request.Dates.Select(x => x.RequestedDate).Order().ToList(),
        request.LeaveType, request.PrivateMessage, request.Status, restaurantClock.ToLocal(request.SubmittedUtc),
        request.ChangedUtc is null ? null : restaurantClock.ToLocal(request.ChangedUtc.Value), request.DecidedBy,
        request.DecisionUtc is null ? null : restaurantClock.ToLocal(request.DecisionUtc.Value), request.DecisionReason,
        request.CancelledUtc is null ? null : restaurantClock.ToLocal(request.CancelledUtc.Value), request.Version);

    private AuditEntry Audit(string actorId, string action, Guid id, string? oldValues, string? newValues,
        string? reason = null) => new()
        {
            ActorId = actorId,
            Action = action,
            EntityType = nameof(LeaveRequest),
            EntityId = id.ToString(),
            OldValuesJson = oldValues,
            NewValuesJson = newValues,
            Reason = reason,
            OccurredUtc = restaurantClock.UtcNow
        };

    // Private request messages are deliberately excluded from the general audit stream.
    private static string Snapshot(LeaveRequest request) => JsonSerializer.Serialize(new
    {
        request.Version,
        Dates = request.Dates.Select(x => x.RequestedDate).Order(),
        request.LeaveType,
        HasPrivateMessage = request.PrivateMessage != null,
        request.Status,
        request.SubmittedUtc,
        request.ChangedUtc,
        request.DecidedBy,
        request.DecisionUtc,
        request.CancelledUtc
    });

    private sealed record StaffActor(string Id, string UserName, string DisplayName);
}
