using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Roms.Application;
using Roms.Domain;
using Roms.Infrastructure.Persistence;

namespace Roms.Infrastructure.Services;

public sealed class ReportService(IDbContextFactory<RomsDbContext> factory) : IReportService
{
    public async Task<DashboardReport> GetDashboardAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var orders = await db.Orders.AsNoTracking().Include(x => x.Items)
            .Where(x => x.Status == OrderStatus.Completed && x.PaymentConfirmedUtc >= fromUtc && x.PaymentConfirmedUtc < toUtc).ToListAsync(ct);
        var value = orders.Sum(x => x.Total);
        var sellers = orders.SelectMany(x => x.Items).Where(x => !x.IsRemoved)
            .GroupBy(x => x.MenuItemName).Select(g => new BestSeller(g.Key, g.Sum(x => x.Quantity), g.Sum(x => x.Quantity * x.UnitPrice)))
            .OrderByDescending(x => x.Quantity).ThenBy(x => x.Name).Take(10).ToList();
        return new DashboardReport(value, orders.Count, orders.Count == 0 ? 0 : value / orders.Count, sellers);
    }
}

public sealed class InventoryService(IDbContextFactory<RomsDbContext> factory, IClock clock) : IInventoryService
{
    public async Task<IReadOnlyList<InventoryBalance>> GetBalancesAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.InventoryItems.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
            .Select(x => new InventoryBalance(x.Id, x.Name, x.Unit, x.Movements.Sum(m => m.QuantityDelta), x.MinimumStock,
                x.Movements.Sum(m => m.QuantityDelta) <= x.MinimumStock)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<InventoryItem>> GetItemsAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.InventoryItems.AsNoTracking().Include(x => x.Movements).OrderBy(x => x.Name).ToListAsync(ct);
    }

    public async Task SaveItemAsync(InventoryItem item, string actorId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(item.Name)) throw new DomainException("Inventory item name is required.");
        if (string.IsNullOrWhiteSpace(item.Unit)) throw new DomainException("Inventory unit is required.");
        if (item.MinimumStock < 0) throw new DomainException("Minimum stock cannot be negative.");
        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureAdminAsync(db, actorId, ct);
        var current = await db.InventoryItems.SingleOrDefaultAsync(x => x.Id == item.Id, ct);
        if (current is null) db.InventoryItems.Add(item);
        else { current.Name = item.Name.Trim(); current.Unit = item.Unit.Trim(); current.MinimumStock = item.MinimumStock; current.IsActive = item.IsActive; }
        db.AuditEntries.Add(new AuditEntry { ActorId = actorId, Action = "SaveInventoryItem", EntityType = nameof(InventoryItem), EntityId = item.Id.ToString(), OccurredUtc = clock.UtcNow });
        await db.SaveChangesAsync(ct);
    }

    public async Task SetRecipeIngredientAsync(Guid menuItemId, Guid inventoryItemId, decimal quantity, string actorId, CancellationToken ct = default)
    {
        if (quantity <= 0) throw new DomainException("Recipe quantity must be greater than zero.");
        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureAdminAsync(db, actorId, ct);
        if (!await db.MenuItems.AnyAsync(x => x.Id == menuItemId, ct) || !await db.InventoryItems.AnyAsync(x => x.Id == inventoryItemId && x.IsActive, ct))
            throw new DomainException("Menu or inventory item not found.");
        var usedByActivePreparation = await db.Orders.AnyAsync(order =>
            (order.Status == OrderStatus.Preparing || order.Status == OrderStatus.Ready) &&
            order.Items.Any(item => !item.IsRemoved && item.MenuItemId == menuItemId), ct);
        if (usedByActivePreparation)
            throw new DomainException("This recipe cannot change while an active order is being prepared.");
        var recipe = await db.RecipeIngredients.SingleOrDefaultAsync(x => x.MenuItemId == menuItemId && x.InventoryItemId == inventoryItemId, ct);
        if (recipe is null) db.RecipeIngredients.Add(new RecipeIngredient { MenuItemId = menuItemId, InventoryItemId = inventoryItemId, Quantity = quantity });
        else recipe.Quantity = quantity;
        db.AuditEntries.Add(new AuditEntry { ActorId = actorId, Action = "SetRecipeIngredient", EntityType = nameof(RecipeIngredient), EntityId = $"{menuItemId}:{inventoryItemId}", Reason = $"Quantity {quantity}", OccurredUtc = clock.UtcNow });
        await db.SaveChangesAsync(ct);
    }

    public async Task AdjustAsync(
        Guid itemId,
        decimal delta,
        string reason,
        string actorId,
        string idempotencyKey,
        CancellationToken ct = default,
        bool allowNegativeStock = false,
        string? inventoryOverrideReason = null)
    {
        if (delta == 0 || string.IsNullOrWhiteSpace(reason)) throw new DomainException("A non-zero quantity and reason are required.");
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 150)
            throw new DomainException("A valid adjustment key is required.");
        await using var strategyContext = await factory.CreateDbContextAsync(ct);
        var strategy = strategyContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            await EnsureAdminAsync(db, actorId, ct);
            if (await db.StockMovements.AnyAsync(x => x.IdempotencyKey == idempotencyKey, ct)) return;
            var item = await db.InventoryItems.SingleOrDefaultAsync(x => x.Id == itemId && x.IsActive, ct)
                ?? throw new DomainException("Inventory item not found.");
            var current = await db.StockMovements.Where(x => x.InventoryItemId == itemId)
                .SumAsync(x => x.QuantityDelta, ct);
            var projected = current + delta;
            if (projected < 0)
            {
                if (!allowNegativeStock)
                    throw new DomainException(
                        $"Insufficient stock: {item.Name} has {current:0.###} {item.Unit}; this adjustment would leave {projected:0.###}.");
                if (string.IsNullOrWhiteSpace(inventoryOverrideReason))
                    throw new DomainException("A manager override reason is required.");
                db.AuditEntries.Add(DiscrepancyAlert(
                    actorId,
                    nameof(InventoryItem),
                    item.Id,
                    inventoryOverrideReason,
                    new { item.Name, item.Unit, Current = current, Delta = delta, Projected = projected }));
            }
            db.StockMovements.Add(new StockMovement
            {
                InventoryItemId = itemId,
                QuantityDelta = delta,
                Type = StockMovementType.Adjustment,
                Reason = reason.Trim(),
                ActorId = actorId,
                IdempotencyKey = idempotencyKey,
                OccurredUtc = clock.UtcNow
            });
            db.AuditEntries.Add(Audit(actorId, "AdjustInventory", nameof(InventoryItem), item.Id,
                reason.Trim(), JsonSerializer.Serialize(new { Delta = delta, Projected = projected })));
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });
    }

    public async Task<IReadOnlyList<InventoryLossRequestView>> GetLossRequestsAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.InventoryLossRequests.AsNoTracking()
            .Include(x => x.InventoryItem)
            .OrderBy(x => x.Status)
            .ThenByDescending(x => x.ReportedUtc)
            .Select(x => new InventoryLossRequestView(
                x.Id,
                x.InventoryItemId,
                x.InventoryItem!.Name,
                x.InventoryItem.Unit,
                x.Type,
                x.Quantity,
                x.Reason,
                x.ReportedBy,
                x.ReportedUtc,
                x.Status,
                x.ReviewedBy,
                x.ReviewedUtc,
                x.ReviewReason))
            .ToListAsync(ct);
    }

    public async Task<Guid> ReportLossAsync(
        Guid itemId,
        InventoryLossType type,
        decimal quantity,
        string reason,
        string actorId,
        string idempotencyKey,
        CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureKitchenOrAdminAsync(db, actorId, ct);
        var existing = await db.InventoryLossRequests.AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, ct);
        if (existing is not null) return existing.Id;
        if (!await db.InventoryItems.AnyAsync(x => x.Id == itemId && x.IsActive, ct))
            throw new DomainException("Inventory item not found.");
        var request = InventoryLossRequest.Report(
            itemId, type, quantity, reason, actorId, idempotencyKey, clock.UtcNow);
        db.InventoryLossRequests.Add(request);
        db.AuditEntries.Add(Audit(actorId, "ReportInventoryLoss", nameof(InventoryLossRequest),
            request.Id, reason.Trim(), JsonSerializer.Serialize(new { Type = type, Quantity = quantity, itemId })));
        await db.SaveChangesAsync(ct);
        return request.Id;
    }

    public async Task ReviewLossAsync(
        Guid requestId,
        bool approve,
        string? reviewReason,
        string adminId,
        CancellationToken ct = default)
    {
        await using var strategyContext = await factory.CreateDbContextAsync(ct);
        var strategy = strategyContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            await EnsureAdminAsync(db, adminId, ct);
            var request = await db.InventoryLossRequests.Include(x => x.InventoryItem)
                .SingleOrDefaultAsync(x => x.Id == requestId, ct)
                ?? throw new DomainException("Loss request not found.");
            if (approve)
            {
                request.Approve(adminId, reviewReason, clock.UtcNow);
                var movementKey = $"loss:{request.Id}:approved";
                if (!await db.StockMovements.AnyAsync(x => x.IdempotencyKey == movementKey, ct))
                {
                    var current = await db.StockMovements
                        .Where(x => x.InventoryItemId == request.InventoryItemId)
                        .SumAsync(x => x.QuantityDelta, ct);
                    var projected = current - request.Quantity;
                    if (projected < 0)
                    {
                        db.AuditEntries.Add(DiscrepancyAlert(
                            adminId,
                            nameof(InventoryLossRequest),
                            request.Id,
                            reviewReason ?? request.Reason,
                            new
                            {
                                request.InventoryItemId,
                                request.InventoryItem!.Name,
                                request.InventoryItem.Unit,
                                Current = current,
                                Loss = request.Quantity,
                                Projected = projected
                            }));
                    }
                    db.StockMovements.Add(new StockMovement
                    {
                        InventoryItemId = request.InventoryItemId,
                        Type = request.Type == InventoryLossType.Waste
                            ? StockMovementType.Waste
                            : StockMovementType.Spoilage,
                        QuantityDelta = -request.Quantity,
                        Reason = request.Reason,
                        ActorId = adminId,
                        IdempotencyKey = movementKey,
                        OccurredUtc = clock.UtcNow
                    });
                }
            }
            else
            {
                request.Reject(adminId, reviewReason ?? string.Empty, clock.UtcNow);
            }
            db.AuditEntries.Add(Audit(adminId,
                approve ? "ApproveInventoryLoss" : "RejectInventoryLoss",
                nameof(InventoryLossRequest),
                request.Id,
                reviewReason?.Trim(),
                JsonSerializer.Serialize(new { request.Type, request.Quantity, request.InventoryItemId })));
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });
    }

    private AuditEntry DiscrepancyAlert(
        string actor,
        string entityType,
        Guid entityId,
        string reason,
        object values) =>
        Audit(actor, "INVENTORY_DISCREPANCY_ALERT", entityType, entityId,
            reason.Trim(), JsonSerializer.Serialize(values));

    private AuditEntry Audit(
        string actor,
        string action,
        string entityType,
        Guid entityId,
        string? reason,
        string? values) => new()
        {
            ActorId = actor,
            Action = action,
            EntityType = entityType,
            EntityId = entityId.ToString(),
            Reason = reason,
            NewValuesJson = values,
            OccurredUtc = clock.UtcNow
        };

    private static async Task EnsureKitchenOrAdminAsync(
        RomsDbContext db, string actorId, CancellationToken ct)
    {
        if (await IsInRoleAsync(db, actorId, RomsRoles.Kitchen, ct) ||
            await IsInRoleAsync(db, actorId, RomsRoles.Admin, ct)) return;
        throw new DomainException("Only Kitchen staff or an administrator can report inventory loss.");
    }

    private static async Task EnsureAdminAsync(RomsDbContext db, string actorId, CancellationToken ct)
    {
        if (await IsInRoleAsync(db, actorId, RomsRoles.Admin, ct)) return;
        throw new DomainException("Only an administrator can manage inventory or approve inventory loss.");
    }

    private static Task<bool> IsInRoleAsync(
        RomsDbContext db, string actorId, string role, CancellationToken ct) =>
        (from user in db.Users
         join userRole in db.UserRoles on user.Id equals userRole.UserId
         join existingRole in db.Roles on userRole.RoleId equals existingRole.Id
         where user.UserName == actorId && existingRole.Name == role
         select user.Id).AnyAsync(ct);
}
