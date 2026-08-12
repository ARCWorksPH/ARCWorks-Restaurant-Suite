using Microsoft.EntityFrameworkCore;
using Roms.Application;
using Roms.Domain;
using Roms.Infrastructure.Persistence;

namespace Roms.Infrastructure.Services;

public sealed class AuditService(IDbContextFactory<RomsDbContext> factory) : IAuditService
{
    public async Task<IReadOnlyList<AuditRecordView>> GetRecentAsync(
        string adminId,
        DateTime fromUtc,
        DateTime toUtc,
        int take = 200,
        CancellationToken ct = default)
    {
        if (toUtc <= fromUtc) throw new DomainException("Audit date range end must be after the start.");
        if (take is < 1 or > 1000) throw new DomainException("Audit result limit must be between 1 and 1000.");

        await using var db = await factory.CreateDbContextAsync(ct);
        var isAdmin = await (from user in db.Users
                             join userRole in db.UserRoles on user.Id equals userRole.UserId
                             join role in db.Roles on userRole.RoleId equals role.Id
                             where user.UserName == adminId && role.Name == RomsRoles.Admin
                             select user.Id).AnyAsync(ct);
        if (!isAdmin) throw new DomainException("Only an administrator can view the audit history.");

        return await db.AuditEntries.AsNoTracking()
            .Where(x => x.OccurredUtc >= fromUtc && x.OccurredUtc < toUtc)
            .OrderByDescending(x => x.OccurredUtc)
            .Take(take)
            .Select(x => new AuditRecordView(x.Id, x.ActorId, x.Action, x.EntityType, x.EntityId,
                x.OldValuesJson, x.NewValuesJson, x.Reason, x.OccurredUtc))
            .ToListAsync(ct);
    }
}
