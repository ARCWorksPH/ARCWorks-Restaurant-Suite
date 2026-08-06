using Microsoft.EntityFrameworkCore;
using Roms.Application;
using Roms.Domain;
using Roms.Infrastructure.Persistence;

namespace Roms.Infrastructure.Services;

public sealed class WorkflowService(IDbContextFactory<RomsDbContext> factory, IClock clock) : IWorkflowService
{
    public async Task<WorkflowSettingsView> GetSettingsAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var settings = await GetOrCreateAsync(db, ct);
        return new(settings.OrderEntryMinutes, settings.KitchenAcceptanceMinutes, settings.UpdatedUtc, settings.UpdatedBy);
    }

    public async Task UpdateSettingsAsync(int orderEntryMinutes, int kitchenAcceptanceMinutes, string actorId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureManagerOrAdminAsync(db, actorId, ct);
        var settings = await GetOrCreateAsync(db, ct);
        settings.Update(orderEntryMinutes, kitchenAcceptanceMinutes, actorId, clock.UtcNow);
        db.AuditEntries.Add(new AuditEntry
        {
            ActorId = actorId, Action = "UpdateWorkflowSettings", EntityType = nameof(WorkflowSettings),
            EntityId = settings.Id.ToString(), OccurredUtc = clock.UtcNow,
            NewValuesJson = $"{{\"orderEntryMinutes\":{orderEntryMinutes},\"kitchenAcceptanceMinutes\":{kitchenAcceptanceMinutes}}}"
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ManagerLiveOrderView>> GetLiveOrdersAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var orders = await db.Orders.AsNoTracking().Include(x => x.Table)
            .Where(x => x.Status != OrderStatus.Completed && x.Status != OrderStatus.Cancelled)
            .OrderBy(x => x.CreatedUtc).ToListAsync(ct);
        var counts = await db.OrderTimerExtensions.AsNoTracking().GroupBy(x => x.OrderId)
            .Select(g => new { OrderId = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.OrderId, x => x.Count, ct);
        return orders.Select(x => new ManagerLiveOrderView(x.Id, x.Table?.Number ?? "?", x.WaiterId, x.Status,
            x.OrderEntryDueUtc, x.KitchenAcceptanceDueUtc, x.PreparationTargetDueUtc,
            counts.TryGetValue(x.Id, out var count) ? count : 0)).ToList();
    }

    private static async Task<WorkflowSettings> GetOrCreateAsync(RomsDbContext db, CancellationToken ct)
    {
        var settings = await db.WorkflowSettings.SingleOrDefaultAsync(ct);
        if (settings is not null) return settings;
        settings = new WorkflowSettings();
        db.WorkflowSettings.Add(settings);
        await db.SaveChangesAsync(ct);
        return settings;
    }

    private static async Task EnsureManagerOrAdminAsync(RomsDbContext db, string actorId, CancellationToken ct)
    {
        var allowed = await (from user in db.Users
                             join userRole in db.UserRoles on user.Id equals userRole.UserId
                             join role in db.Roles on userRole.RoleId equals role.Id
                             where user.UserName == actorId && (role.Name == RomsRoles.Manager || role.Name == RomsRoles.Admin)
                             select user.Id).AnyAsync(ct);
        if (!allowed) throw new DomainException("Only a manager or administrator can change workflow settings.");
    }
}
