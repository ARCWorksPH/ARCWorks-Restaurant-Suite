using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Roms.Application;
using Roms.Domain;
using Roms.Infrastructure.Persistence;

namespace Roms.Infrastructure.Services;

public sealed class OrderService(
    IDbContextFactory<RomsDbContext> factory,
    IClock clock,
    IOrderEventPublisher publisher,
    ILogger<OrderService> logger) : IOrderService
{
    private static readonly OrderStatus[] ActiveStatuses = [OrderStatus.Draft, OrderStatus.New, OrderStatus.ReturnedToWaiter, OrderStatus.Preparing, OrderStatus.Ready];

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
        var order = await db.Orders.AsNoTracking().Include(x => x.Table).Include(x => x.Items).Include(x => x.StatusHistory).SingleOrDefaultAsync(x => x.Id == orderId, ct);
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
            if (active.Status == OrderStatus.Draft && active.OrderEntryDueUtc is null)
            {
                var existingSettings = await GetOrCreateWorkflowSettingsAsync(db, ct);
                active.StartOrderEntryTimer(existingSettings.OrderEntryMinutes, clock.UtcNow);
                await db.SaveChangesAsync(ct);
            }
            return active.Id;
        }
        if (!await db.RestaurantTables.AnyAsync(x => x.Id == tableId && x.IsActive, ct)) throw new DomainException("Table not found.");
        var order = new Order { TableId = tableId, WaiterId = waiterId, CreatedUtc = clock.UtcNow };
        var settings = await GetOrCreateWorkflowSettingsAsync(db, ct);
        order.StartOrderEntryTimer(settings.OrderEntryMinutes, clock.UtcNow);
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
        CancellationToken ct = default)
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
        CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var order = await db.Orders.Include(x => x.Items).SingleAsync(x => x.Id == orderId, ct);
        await EnsureOwnerOrAdminAsync(db, order, actorId, ct);
        var verifiedIsAdmin = await IsInRoleAsync(db, actorId, RomsRoles.Admin, ct);
        order.AmendRemoveItem(itemId, actorId, reason, verifiedIsAdmin, clock.UtcNow);
        db.AuditEntries.Add(Audit(actorId, "AmendRemoveOrderItem", order.Id,
            JsonSerializer.Serialize(new { itemId, reason, order.Revision })));
        await db.SaveChangesAsync(ct);
        await Publish(order, "OrderAmended", ct);
    }

    public async Task<Guid> SubmitAsync(Guid orderId, string idempotencyKey, string actorId, string? resubmissionNote = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 100) throw new DomainException("A valid submission key is required.");
        await using var db = await factory.CreateDbContextAsync(ct);
        var existing = await db.IdempotencyRecords.AsNoTracking().SingleOrDefaultAsync(x => x.Key == idempotencyKey, ct);
        var order = await db.Orders.Include(x => x.Items).SingleAsync(x => x.Id == orderId, ct);
        await EnsureOwnerOrAdminAsync(db, order, actorId, ct);
        if (existing is not null) return existing.ResourceId;
        order.Submit(clock.UtcNow, resubmissionNote);
        var settings = await GetOrCreateWorkflowSettingsAsync(db, ct);
        order.StartKitchenAcceptanceTimer(settings.KitchenAcceptanceMinutes, clock.UtcNow);
        db.IdempotencyRecords.Add(new IdempotencyRecord { Key = idempotencyKey, Operation = "SubmitOrder", ResourceId = order.Id, CreatedUtc = clock.UtcNow });
        db.AuditEntries.Add(Audit(actorId, order.ResubmissionCount > 0 ? "ResubmitOrder" : "SubmitOrder", order.Id,
            string.IsNullOrWhiteSpace(resubmissionNote) ? null : JsonSerializer.Serialize(new { resubmissionNote, order.ResubmissionCount })));
        await db.SaveChangesAsync(ct);
        await Publish(order, "OrderSubmitted", ct);
        return order.Id;
    }

    public async Task TransitionAsync(
        Guid orderId,
        OrderStatus next,
        string actorId,
        string? reason = null,
        CancellationToken ct = default)
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
                else if (next is OrderStatus.Preparing or OrderStatus.ReturnedToWaiter or OrderStatus.Ready)
                    await EnsureKitchenOrAdminAsync(db, actorId, ct);
                else
                    await EnsureOwnerOrAdminAsync(db, order, actorId, ct);
                order.TransitionTo(next, actorId, reason, clock.UtcNow);

                if (next == OrderStatus.Preparing)
                {
                    var itemIds = order.Items.Where(x => !x.IsRemoved).Select(x => x.MenuItemId).Distinct().ToList();
                    var preparationMinutes = await db.MenuItems.AsNoTracking()
                        .Where(x => itemIds.Contains(x.Id))
                        .ToDictionaryAsync(x => x.Id, x => x.PreparationMinutes, ct);
                    if (preparationMinutes.Count != itemIds.Count)
                        throw new DomainException("One or more ordered menu items no longer has a preparation target.");
                    var target = order.Items.Where(x => !x.IsRemoved)
                        .Sum(x => checked(x.Quantity * preparationMinutes[x.MenuItemId]));
                    order.SetPreparationTarget(target, clock.UtcNow);
                }

                db.AuditEntries.Add(Audit(actorId, next == OrderStatus.Cancelled ? "CancelOrder" : "ChangeOrderStatus", order.Id,
                    JsonSerializer.Serialize(new
                    {
                        from = previous,
                        to = next,
                        reason
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
                "Another order update happened at the same time. Reload and try this action again.");
        }
        await Publish(changedOrder!, "OrderStatusChanged", ct);
    }

    public async Task RequestTimerExtensionAsync(Guid orderId, WorkflowTimerKind kind, int additionalMinutes, string reason, string actorId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var order = await db.Orders.Include(x => x.Items).SingleAsync(x => x.Id == orderId, ct);
        if (kind == WorkflowTimerKind.OrderEntry) await EnsureOwnerOrAdminAsync(db, order, actorId, ct);
        else await EnsureKitchenOrAdminAsync(db, actorId, ct);
        order.ExtendTimer(kind, additionalMinutes, reason, clock.UtcNow);
        var count = await db.OrderTimerExtensions.CountAsync(x => x.OrderId == orderId && x.Kind == kind, ct) + 1;
        db.OrderTimerExtensions.Add(new OrderTimerExtension
        {
            OrderId = orderId, Kind = kind, AdditionalMinutes = additionalMinutes,
            ExtensionCount = count, Reason = reason.Trim(), ActorId = actorId, RequestedUtc = clock.UtcNow
        });
        db.AuditEntries.Add(Audit(actorId, "RequestTimerExtension", orderId,
            JsonSerializer.Serialize(new { kind, additionalMinutes, reason, extensionCount = count })));
        await db.SaveChangesAsync(ct);
        await Publish(order, "OrderTimerExtended", ct);
    }

    private async Task<WorkflowSettings> GetOrCreateWorkflowSettingsAsync(RomsDbContext db, CancellationToken ct)
    {
        var settings = await db.WorkflowSettings.SingleOrDefaultAsync(ct);
        if (settings is not null) return settings;
        settings = new WorkflowSettings();
        db.WorkflowSettings.Add(settings);
        await db.SaveChangesAsync(ct);
        return settings;
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
        OrderStatus.Draft or OrderStatus.New or OrderStatus.ReturnedToWaiter => TableStatus.Occupied,
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
        x.CancellationReason, x.StatusHistory.OrderByDescending(h => h.OccurredUtc).FirstOrDefault(h => h.ToStatus == OrderStatus.ReturnedToWaiter)?.Reason,
        x.ResubmissionCount, x.PreparationTargetMinutes, x.PreparationTargetDueUtc,
        x.OrderEntryTargetMinutes, x.OrderEntryStartedUtc, x.OrderEntryDueUtc,
        x.KitchenAcceptanceTargetMinutes, x.KitchenAcceptanceStartedUtc, x.KitchenAcceptanceDueUtc,
        x.Items.OrderBy(i => i.MenuItemName).Select(i => new OrderItemView(
            i.Id,
            i.MenuItemName,
            i.UnitPrice,
            i.Quantity,
            i.Notes,
            i.IsRemoved)).ToList());
}
