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
        if (string.IsNullOrWhiteSpace(item.Name) || string.IsNullOrWhiteSpace(item.Unit) || item.MinimumStock < 0)
            throw new DomainException("Name, unit, and a non-negative minimum stock are required.");
        await using var db = await factory.CreateDbContextAsync(ct);
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
        if (!await db.MenuItems.AnyAsync(x => x.Id == menuItemId, ct) || !await db.InventoryItems.AnyAsync(x => x.Id == inventoryItemId && x.IsActive, ct))
            throw new DomainException("Menu or inventory item not found.");
        var recipe = await db.RecipeIngredients.SingleOrDefaultAsync(x => x.MenuItemId == menuItemId && x.InventoryItemId == inventoryItemId, ct);
        if (recipe is null) db.RecipeIngredients.Add(new RecipeIngredient { MenuItemId = menuItemId, InventoryItemId = inventoryItemId, Quantity = quantity });
        else recipe.Quantity = quantity;
        db.AuditEntries.Add(new AuditEntry { ActorId = actorId, Action = "SetRecipeIngredient", EntityType = nameof(RecipeIngredient), EntityId = $"{menuItemId}:{inventoryItemId}", Reason = $"Quantity {quantity}", OccurredUtc = clock.UtcNow });
        await db.SaveChangesAsync(ct);
    }

    public async Task AdjustAsync(Guid itemId, decimal delta, string reason, string actorId, string idempotencyKey, CancellationToken ct = default)
    {
        if (delta == 0 || string.IsNullOrWhiteSpace(reason)) throw new DomainException("A non-zero quantity and reason are required.");
        await using var db = await factory.CreateDbContextAsync(ct);
        if (await db.StockMovements.AnyAsync(x => x.IdempotencyKey == idempotencyKey, ct)) return;
        if (!await db.InventoryItems.AnyAsync(x => x.Id == itemId && x.IsActive, ct)) throw new DomainException("Inventory item not found.");
        db.StockMovements.Add(new StockMovement { InventoryItemId = itemId, QuantityDelta = delta, Type = StockMovementType.Adjustment,
            Reason = reason.Trim(), ActorId = actorId, IdempotencyKey = idempotencyKey, OccurredUtc = clock.UtcNow });
        await db.SaveChangesAsync(ct);
    }
}
