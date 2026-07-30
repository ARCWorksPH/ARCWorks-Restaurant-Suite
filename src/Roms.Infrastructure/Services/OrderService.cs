using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using Roms.Application;
using Roms.Domain;
using Roms.Infrastructure.Persistence;

namespace Roms.Infrastructure.Services;

public sealed class InventoryOptions { public bool Enabled { get; set; } }

public sealed class OrderService(
    IDbContextFactory<RomsDbContext> factory,
    IClock clock,
    IOrderEventPublisher publisher,
    IOptions<InventoryOptions> inventoryOptions,
    ILogger<OrderService> logger) : IOrderService
{
    private static readonly OrderStatus[] ActiveStatuses = [OrderStatus.Draft, OrderStatus.New, OrderStatus.Preparing, OrderStatus.Ready];

    public async Task<IReadOnlyList<TableCard>> GetTablesAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var tables = await db.RestaurantTables.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.Number).ToListAsync(ct);
        var orders = await db.Orders.AsNoTracking().Include(x => x.Items)
            .Where(x => ActiveStatuses.Contains(x.Status) || (x.Status == OrderStatus.Completed && x.PaymentConfirmedUtc == null)).ToListAsync(ct);
        var waiterNames = await GetWaiterNamesAsync(db, orders.Select(x => x.WaiterId), ct);
        return tables.Select(t =>
        {
            var order = orders.Where(x => x.TableId == t.Id).OrderByDescending(x => x.CreatedUtc).FirstOrDefault();
            return new TableCard(t.Id, t.Number, ToTableStatus(order), order?.Id, order?.Total ?? 0m,
                order?.WaiterId, order is null ? null : WaiterName(order.WaiterId, waiterNames));
        }).ToList();
    }

    public async Task<IReadOnlyList<MenuItemChoice>> GetMenuAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.MenuItems.AsNoTracking().Where(x => x.IsActive && x.IsAvailable && x.Category!.IsActive)
            .OrderBy(x => x.Category!.SortOrder).ThenBy(x => x.Name)
            .Select(x => new MenuItemChoice(x.Id, x.Name, x.Category!.Name, x.Price, x.Description)).ToListAsync(ct);
    }

    public async Task<OrderView?> GetOrderAsync(Guid orderId, string actorId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var order = await db.Orders.AsNoTracking().Include(x => x.Table).Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == orderId, ct);
        if (order is null) return null;
        await EnsureOwnerOrAdminAsync(db, order, actorId, ct);
        var waiterNames = await GetWaiterNamesAsync(db, [order.WaiterId], ct);
        return Map(order, waiterNames);
    }

    public async Task<IReadOnlyList<OrderView>> GetKitchenOrdersAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var orders = await db.Orders.AsNoTracking().Include(x => x.Table).Include(x => x.Items)
            .Where(x => x.Status == OrderStatus.New || x.Status == OrderStatus.Preparing || x.Status == OrderStatus.Ready)
            .OrderBy(x => x.SubmittedUtc).ToListAsync(ct);
        var waiterNames = await GetWaiterNamesAsync(db, orders.Select(x => x.WaiterId), ct);
        return orders.Select(x => Map(x, waiterNames)).ToList();
    }

    public async Task<IReadOnlyList<OrderView>> GetPendingPaymentsAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var orders = await db.Orders.AsNoTracking().Include(x => x.Table).Include(x => x.Items)
            .Where(x => x.Status == OrderStatus.Completed && x.PaymentConfirmedUtc == null)
            .OrderBy(x => x.CompletedUtc).ToListAsync(ct);
        var waiterNames = await GetWaiterNamesAsync(db, orders.Select(x => x.WaiterId), ct);
        return orders.Select(x => Map(x, waiterNames)).ToList();
    }

    public async Task<Guid> GetOrCreateDraftAsync(Guid tableId, string waiterId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var active = await db.Orders.Include(x => x.Table).Where(x => x.TableId == tableId &&
                (ActiveStatuses.Contains(x.Status) || (x.Status == OrderStatus.Completed && x.PaymentConfirmedUtc == null)))
            .OrderByDescending(x => x.CreatedUtc).FirstOrDefaultAsync(ct);
        if (active is not null)
        {
            await EnsureOwnerOrAdminAsync(db, active, waiterId, ct);
            return active.Id;
        }
        if (!await db.RestaurantTables.AnyAsync(x => x.Id == tableId && x.IsActive, ct)) throw new DomainException("Table not found.");
        var order = new Order { TableId = tableId, WaiterId = waiterId, CreatedUtc = clock.UtcNow };
        db.Orders.Add(order);
        db.AuditEntries.Add(Audit(waiterId, "CreateDraft", order.Id, null));
        await db.SaveChangesAsync(ct);
        await Publish(order, "OrderCreated", ct);
        return order.Id;
    }

    public async Task AddItemAsync(Guid orderId, Guid menuItemId, int quantity, string? notes, string actorId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var order = await db.Orders.Include(x => x.Items).SingleAsync(x => x.Id == orderId, ct);
        await EnsureOwnerOrAdminAsync(db, order, actorId, ct);
        var menuItem = await db.MenuItems.SingleAsync(x => x.Id == menuItemId, ct);
        order.AddItem(menuItem, quantity, notes, clock.UtcNow);
        db.AuditEntries.Add(Audit(actorId, "AddOrderItem", order.Id, JsonSerializer.Serialize(new { menuItemId, quantity, notes })));
        await db.SaveChangesAsync(ct);
        await Publish(order, "OrderAmended", ct);
    }

    public async Task RemoveDraftItemAsync(Guid orderId, Guid itemId, string actorId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var order = await db.Orders.Include(x => x.Items).SingleAsync(x => x.Id == orderId, ct);
        await EnsureOwnerOrAdminAsync(db, order, actorId, ct);
        order.RemoveDraftItem(itemId, clock.UtcNow);
        db.AuditEntries.Add(Audit(actorId, "RemoveOrderItem", order.Id, JsonSerializer.Serialize(new { itemId })));
        await db.SaveChangesAsync(ct);
        await Publish(order, "OrderAmended", ct);
    }

    public async Task AmendAddItemAsync(
        Guid orderId,
        Guid menuItemId,
        int quantity,
        string? notes,
        string reason,
        string actorId,
        CancellationToken ct = default,
        bool allowNegativeStock = false,
        string? inventoryOverrideReason = null)
    {
        await using var strategyContext = await factory.CreateDbContextAsync(ct);
        var strategy = strategyContext.Database.CreateExecutionStrategy();
        Order? changedOrder = null;
        await strategy.ExecuteAsync(async () =>
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var order = await db.Orders.Include(x => x.Items).SingleAsync(x => x.Id == orderId, ct);
            await EnsureOwnerOrAdminAsync(db, order, actorId, ct);
            var menuItem = await db.MenuItems.SingleAsync(x => x.Id == menuItemId, ct);
            order.AmendAddItem(menuItem, quantity, notes, actorId, reason, clock.UtcNow);
            if (inventoryOptions.Value.Enabled && order.Status == OrderStatus.Preparing)
                await ReconcileInventoryAsync(db, order, actorId, consume: true,
                    $"Order {order.Id} revision {order.Revision} added an item", $"revision:{order.Revision}", ct,
                    allowNegativeStock, inventoryOverrideReason);
            db.AuditEntries.Add(Audit(actorId, "AmendAddOrderItem", order.Id,
                JsonSerializer.Serialize(new { menuItemId, quantity, notes, reason, order.Revision })));
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            changedOrder = order;
        });
        await Publish(changedOrder!, "OrderAmended", ct);
    }

    public async Task AmendRemoveItemAsync(
        Guid orderId,
        Guid itemId,
        string reason,
        string actorId,
        CancellationToken ct = default,
        InventoryDisposition? inventoryDisposition = null)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var order = await db.Orders.Include(x => x.Items).SingleAsync(x => x.Id == orderId, ct);
        await EnsureOwnerOrAdminAsync(db, order, actorId, ct);
        var verifiedIsAdmin = await IsInRoleAsync(db, actorId, RomsRoles.Admin, ct);
        order.AmendRemoveItem(itemId, actorId, reason, verifiedIsAdmin, inventoryDisposition, clock.UtcNow);
        var effectiveDisposition = order.Items.Single(x => x.Id == itemId).RemovalInventoryDisposition;
        if (inventoryOptions.Value.Enabled && order.Status == OrderStatus.Preparing)
            await ReconcileInventoryAsync(db, order, actorId, consume: true,
                $"Order {order.Id} revision {order.Revision} removed an item", $"revision:{order.Revision}", ct);
        db.AuditEntries.Add(Audit(actorId, "AmendRemoveOrderItem", order.Id,
            JsonSerializer.Serialize(new
            {
                itemId,
                reason,
                inventoryDisposition = effectiveDisposition?.ToString(),
                order.Revision
            })));
        await db.SaveChangesAsync(ct);
        await Publish(order, "OrderAmended", ct);
    }

    public async Task<Guid> SubmitAsync(Guid orderId, string idempotencyKey, string actorId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 100) throw new DomainException("A valid submission key is required.");
        await using var db = await factory.CreateDbContextAsync(ct);
        var existing = await db.IdempotencyRecords.AsNoTracking().SingleOrDefaultAsync(x => x.Key == idempotencyKey, ct);
        var order = await db.Orders.Include(x => x.Items).SingleAsync(x => x.Id == orderId, ct);
        await EnsureOwnerOrAdminAsync(db, order, actorId, ct);
        if (existing is not null) return existing.ResourceId;
        order.Submit(clock.UtcNow);
        db.IdempotencyRecords.Add(new IdempotencyRecord { Key = idempotencyKey, Operation = "SubmitOrder", ResourceId = order.Id, CreatedUtc = clock.UtcNow });
        db.AuditEntries.Add(Audit(actorId, "SubmitOrder", order.Id, null));
        await db.SaveChangesAsync(ct);
        await Publish(order, "OrderSubmitted", ct);
        return order.Id;
    }

    public async Task TransitionAsync(
        Guid orderId,
        OrderStatus next,
        string actorId,
        string? reason = null,
        CancellationToken ct = default,
        InventoryDisposition? inventoryDisposition = null,
        bool allowNegativeStock = false,
        string? inventoryOverrideReason = null)
    {
        await using var strategyContext = await factory.CreateDbContextAsync(ct);
        var strategy = strategyContext.Database.CreateExecutionStrategy();
        Order? changedOrder = null;
        try
        {
            await strategy.ExecuteAsync(async () =>
            {
                await using var db = await factory.CreateDbContextAsync(ct);
                await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
                var order = await db.Orders.Include(x => x.Items).SingleAsync(x => x.Id == orderId, ct);
                var previous = order.Status;
                if (next == OrderStatus.Cancelled)
                {
                    if (previous is OrderStatus.Preparing or OrderStatus.Ready) await EnsureAdminAsync(db, actorId, ct);
                    else await EnsureOwnerOrAdminAsync(db, order, actorId, ct);
                }
                else if (next == OrderStatus.Completed)
                    await EnsureOwnerOrAdminAsync(db, order, actorId, ct);
                else
                    await EnsureKitchenOrAdminAsync(db, actorId, ct);
                order.TransitionTo(next, actorId, reason, clock.UtcNow, inventoryDisposition);

                if (inventoryOptions.Value.Enabled)
                {
                    if (next == OrderStatus.Preparing)
                        await ReconcileInventoryAsync(db, order, actorId, consume: true,
                            $"Order {order.Id} entered Preparing", "preparing", ct,
                            allowNegativeStock, inventoryOverrideReason);
                    else if (next == OrderStatus.Cancelled &&
                             previous is OrderStatus.Preparing or OrderStatus.Ready &&
                             order.CancellationInventoryDisposition == InventoryDisposition.ReturnToStock)
                        await ReconcileInventoryAsync(db, order, actorId, consume: false,
                            $"Order {order.Id} was cancelled: {reason}", "cancelled", ct);
                }

                db.AuditEntries.Add(Audit(actorId, next == OrderStatus.Cancelled ? "CancelOrder" : "ChangeOrderStatus", order.Id,
                    JsonSerializer.Serialize(new
                    {
                        from = previous,
                        to = next,
                        reason,
                        inventoryDisposition = order.CancellationInventoryDisposition?.ToString()
                    })));
                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                changedOrder = order;
            });
        }
        catch (Exception exception) when (IsTransientTransactionConflict(exception))
        {
            logger.LogWarning(exception,
                "Order {OrderId} hit a transient database concurrency conflict.", orderId);
            throw new DomainException(
                "Another inventory update happened at the same time. Reload and try this action again.");
        }
        await Publish(changedOrder!, "OrderStatusChanged", ct);
    }

    public async Task ConfirmPaymentAsync(Guid orderId, string adminId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var order = await db.Orders.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == orderId, ct)
            ?? throw new DomainException("Order not found.");
        await EnsureAdminAsync(db, adminId, ct);
        order.ConfirmPayment(adminId, clock.UtcNow);
        db.AuditEntries.Add(Audit(adminId, "ConfirmPayment", order.Id,
            JsonSerializer.Serialize(new { order.PaymentConfirmedUtc, order.Total })));
        await db.SaveChangesAsync(ct);
        await Publish(order, "PaymentConfirmed", ct);
    }

    private async Task ReconcileInventoryAsync(
        RomsDbContext db,
        Order order,
        string actorId,
        bool consume,
        string reason,
        string operation,
        CancellationToken ct,
        bool allowNegativeStock = false,
        string? inventoryOverrideReason = null)
    {
        var desired = new Dictionary<Guid, decimal>();
        if (consume)
        {
            var consumedItems = order.Items
                .Where(x => !x.IsRemoved ||
                            x.RemovalInventoryDisposition == InventoryDisposition.ConsumedAsWasteOrStaffMeal)
                .ToList();
            var menuIds = consumedItems.Select(x => x.MenuItemId).Distinct().ToList();
            var recipes = new List<RecipeIngredient>();
            foreach (var menuId in menuIds)
            {
                recipes.AddRange(await db.RecipeIngredients
                    .Where(x => x.MenuItemId == menuId)
                    .ToListAsync(ct));
            }
            foreach (var group in recipes.GroupBy(x => x.InventoryItemId))
                desired[group.Key] = -consumedItems.Join(group, i => i.MenuItemId, r => r.MenuItemId,
                    (i, r) => i.Quantity * r.Quantity).Sum();
        }

        var existing = await db.StockMovements.Where(x => x.OrderId == order.Id).ToListAsync(ct);
        var inventoryItemIds = desired.Keys.Concat(existing.Select(x => x.InventoryItemId)).Distinct().ToList();
        var planned = new List<StockMovement>();
        foreach (var inventoryItemId in inventoryItemIds)
        {
            var current = existing.Where(x => x.InventoryItemId == inventoryItemId).Sum(x => x.QuantityDelta);
            var target = desired.GetValueOrDefault(inventoryItemId);
            var delta = target - current;
            if (delta == 0) continue;

            var key = operation == "preparing"
                ? $"order:{order.Id}:preparing:{inventoryItemId}"
                : $"order:{order.Id}:{operation}:{inventoryItemId}";
            if (await db.StockMovements.AnyAsync(x => x.IdempotencyKey == key, ct)) continue;
            planned.Add(new StockMovement { InventoryItemId = inventoryItemId,
                Type = delta < 0 ? StockMovementType.Consumption : StockMovementType.Reversal,
                QuantityDelta = delta, Reason = reason, OrderId = order.Id,
                IdempotencyKey = key, ActorId = actorId, OccurredUtc = clock.UtcNow });
        }

        var negativeDeltas = planned.Where(x => x.QuantityDelta < 0).ToList();
        if (negativeDeltas.Count > 0)
        {
            var negativeIds = negativeDeltas.Select(x => x.InventoryItemId).Distinct().ToList();
            var balances = new Dictionary<Guid, decimal>();
            var inventoryItems = new Dictionary<Guid, InventoryItem>();
            // Connector/NET cannot reliably type-map parameterized Guid collections on MariaDB.
            // The recipe set for one order is small, and individual reads also establish the
            // serializable-range locks needed to prevent two tickets spending the same stock.
            foreach (var inventoryItemId in negativeIds)
            {
                balances[inventoryItemId] = await db.StockMovements
                    .Where(x => x.InventoryItemId == inventoryItemId)
                    .SumAsync(x => x.QuantityDelta, ct);
                inventoryItems[inventoryItemId] = await db.InventoryItems
                    .SingleAsync(x => x.Id == inventoryItemId, ct);
            }
            var shortages = negativeDeltas
                .Select(x => new
                {
                    x.InventoryItemId,
                    Required = -x.QuantityDelta,
                    Available = balances.GetValueOrDefault(x.InventoryItemId),
                    Projected = balances.GetValueOrDefault(x.InventoryItemId) + x.QuantityDelta
                })
                .Where(x => x.Projected < 0)
                .ToList();

            if (shortages.Count > 0)
            {
                var details = string.Join("; ", shortages.Select(x =>
                {
                    var item = inventoryItems[x.InventoryItemId];
                    return $"{item.Name} needs {x.Required:0.###} {item.Unit} but {x.Available:0.###} is available";
                }));
                if (!allowNegativeStock)
                    throw new DomainException($"Insufficient stock: {details}.");

                await EnsureAdminAsync(db, actorId, ct);
                order.RecordInventoryOverride(actorId, inventoryOverrideReason ?? string.Empty, clock.UtcNow);
                db.AuditEntries.Add(new AuditEntry
                {
                    ActorId = actorId,
                    Action = "INVENTORY_DISCREPANCY_ALERT",
                    EntityType = nameof(Order),
                    EntityId = order.Id.ToString(),
                    Reason = inventoryOverrideReason!.Trim(),
                    NewValuesJson = JsonSerializer.Serialize(shortages),
                    OccurredUtc = clock.UtcNow
                });
            }
        }

        db.StockMovements.AddRange(planned);
    }

    private AuditEntry Audit(string actor, string action, Guid id, string? values) => new()
        { ActorId = actor, Action = action, EntityType = nameof(Order), EntityId = id.ToString(), NewValuesJson = values, OccurredUtc = clock.UtcNow };

    private async Task Publish(Order order, string kind, CancellationToken ct)
    {
        try
        {
            await publisher.PublishAsync(
                new OrderEvent(order.Id, order.Revision, order.Version, clock.UtcNow, kind),
                ct);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Order event {EventKind} for order {OrderId} could not be delivered after the state was committed. Clients must reload authoritative state.",
                kind,
                order.Id);
        }
    }

    private static TableStatus ToTableStatus(Order? order) => order?.Status switch
    {
        OrderStatus.Completed when order.PaymentConfirmedUtc is null => TableStatus.PendingPayment,
        OrderStatus.Preparing => TableStatus.Preparing,
        OrderStatus.Ready => TableStatus.ReadyToServe,
        OrderStatus.Draft or OrderStatus.New => TableStatus.Occupied,
        _ => TableStatus.Available
    };

    private static async Task EnsureOwnerOrAdminAsync(RomsDbContext db, Order order, string actorId, CancellationToken ct)
    {
        if (string.Equals(order.WaiterId, actorId, StringComparison.OrdinalIgnoreCase) || await IsInRoleAsync(db, actorId, RomsRoles.Admin, ct)) return;
        var names = await GetWaiterNamesAsync(db, [order.WaiterId], ct);
        throw new DomainException($"Table {order.Table?.Number ?? "order"} is assigned to {WaiterName(order.WaiterId, names)}.");
    }

    private static async Task EnsureKitchenOrAdminAsync(RomsDbContext db, string actorId, CancellationToken ct)
    {
        if (await IsInRoleAsync(db, actorId, RomsRoles.Kitchen, ct) || await IsInRoleAsync(db, actorId, RomsRoles.Admin, ct)) return;
        throw new DomainException("Only Kitchen staff or an administrator can change the kitchen status.");
    }

    private static async Task EnsureAdminAsync(RomsDbContext db, string actorId, CancellationToken ct)
    {
        if (await IsInRoleAsync(db, actorId, RomsRoles.Admin, ct)) return;
        throw new DomainException("Only an administrator can perform this protected action.");
    }

    private static Task<bool> IsInRoleAsync(RomsDbContext db, string actorId, string role, CancellationToken ct) =>
        (from user in db.Users
         join userRole in db.UserRoles on user.Id equals userRole.UserId
         join existingRole in db.Roles on userRole.RoleId equals existingRole.Id
         where user.UserName == actorId && existingRole.Name == role
         select user.Id).AnyAsync(ct);

    private static bool IsTransientTransactionConflict(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is MySqlException { Number: 1205 or 1213 })
                return true;
        }
        return false;
    }

    private static async Task<Dictionary<string, string>> GetWaiterNamesAsync(RomsDbContext db, IEnumerable<string> waiterIds, CancellationToken ct)
    {
        var ids = waiterIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (ids.Count == 0) return new(StringComparer.OrdinalIgnoreCase);
        // Connector/NET cannot type-map a parameterized string collection in this query on MariaDB.
        // A restaurant has a small staff roster, so fetch the two required columns and filter in memory.
        var users = await db.Users.AsNoTracking().Where(x => x.UserName != null)
            .Select(x => new { x.UserName, x.DisplayName }).ToListAsync(ct);
        return users.Where(x => ids.Contains(x.UserName!)).ToDictionary(
            x => x.UserName!, x => string.IsNullOrWhiteSpace(x.DisplayName) ? x.UserName! : x.DisplayName,
            StringComparer.OrdinalIgnoreCase);
    }

    private static string WaiterName(string waiterId, IReadOnlyDictionary<string, string> waiterNames) =>
        waiterNames.TryGetValue(waiterId, out var displayName) ? displayName : waiterId;

    private static OrderView Map(Order x, IReadOnlyDictionary<string, string> waiterNames) => new(
        x.Id, x.TableId, x.Table?.Number ?? "?", x.WaiterId, WaiterName(x.WaiterId, waiterNames), x.Status,
        x.CreatedUtc, x.SubmittedUtc, x.CompletedUtc, x.PaymentConfirmedUtc, x.Revision, x.Version, x.Total,
        x.CancellationReason,
        x.CancellationInventoryDisposition,
        x.InventoryOverrideReason,
        x.InventoryOverriddenBy,
        x.InventoryOverrideUtc,
        x.Items.OrderBy(i => i.MenuItemName).Select(i => new OrderItemView(
            i.Id,
            i.MenuItemName,
            i.UnitPrice,
            i.Quantity,
            i.Notes,
            i.IsRemoved,
            i.RemovalInventoryDisposition)).ToList());
}
