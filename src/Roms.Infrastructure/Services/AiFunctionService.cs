using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Roms.Application;
using Roms.Application.Ai;
using Roms.Domain;
using Roms.Infrastructure.Persistence;

namespace Roms.Infrastructure.Services;

public sealed class AiFunctionService(
    IDbContextFactory<RomsDbContext> factory,
    IClock clock) : IAiFunctionService
{
    private static readonly OrderStatus[] KitchenStatuses =
        [OrderStatus.New, OrderStatus.Preparing, OrderStatus.Ready];

    public async Task<AiFunctionResponse> ExecuteAsync(
        AiFunctionRequest request,
        string actorUsername,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(actorUsername))
            return Response(request, AiFunctionStatus.Unauthorized, "Authentication is required.");

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var roles = await GetRolesAsync(db, actorUsername, cancellationToken);
        if (roles.Count == 0)
            return await FinishAsync(db, request, actorUsername,
                Response(request, AiFunctionStatus.Unauthorized, "You do not have permission to use this function."),
                cancellationToken);

        AiFunctionResponse response;
        try
        {
            response = request.Function switch
            {
                AiFunctionName.GetMenuItem => await GetMenuItemAsync(db, request, roles, cancellationToken),
                AiFunctionName.ListMenu => await ListMenuAsync(db, request, roles, cancellationToken),
                AiFunctionName.GetInventoryBalance => await GetInventoryBalanceAsync(db, request, roles, cancellationToken),
                AiFunctionName.ListInventoryBalances => await ListInventoryAsync(db, request, roles, false, cancellationToken),
                AiFunctionName.ListLowStockItems => await ListInventoryAsync(db, request, roles, true, cancellationToken),
                AiFunctionName.GetOrderStatus => await GetOrderStatusAsync(db, request, actorUsername, roles, cancellationToken),
                AiFunctionName.ListOrdersByStatus => await ListOrdersByStatusAsync(db, request, actorUsername, roles, cancellationToken),
                AiFunctionName.GetDailyOrderSummary => await GetDailyOrderSummaryAsync(db, request, roles, cancellationToken),
                AiFunctionName.GetOrderStatusSummary => await GetOrderStatusSummaryAsync(db, request, roles, cancellationToken),
                AiFunctionName.GetLowStockSummary => await GetLowStockSummaryAsync(db, request, roles, cancellationToken),
                AiFunctionName.GetMenuAvailabilitySummary => await GetMenuAvailabilitySummaryAsync(db, request, roles, cancellationToken),
                AiFunctionName.GetOperationalSummary => await GetOperationalSummaryAsync(db, request, roles, cancellationToken),
                _ => Response(request, AiFunctionStatus.Unsupported, "This function is not approved.")
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            response = Response(request, AiFunctionStatus.InvalidRequest, exception.Message);
        }

        return await FinishAsync(db, request, actorUsername, response, cancellationToken);
    }

    private static async Task<AiFunctionResponse> GetMenuItemAsync(
        RomsDbContext db,
        AiFunctionRequest request,
        IReadOnlySet<string> roles,
        CancellationToken ct)
    {
        if (!CanUseAssistant(roles))
            return Response(request, AiFunctionStatus.Unauthorized,
                "You do not have permission to view menu information.");
        if (string.IsNullOrWhiteSpace(request.ItemName))
            return Response(request, AiFunctionStatus.InvalidRequest, "A menu item name is required.");

        var normalized = request.ItemName.Trim();
        var matches = await db.MenuItems.AsNoTracking()
            .Where(x => x.IsActive && x.Category!.IsActive && x.Name.ToUpper() == normalized.ToUpper())
            .OrderBy(x => x.Name)
            .Take(2)
            .Select(x => new
            {
                x.Id,
                x.Name,
                Category = x.Category!.Name,
                x.Description,
                x.Price,
                x.IsAvailable
            })
            .ToListAsync(ct);

        if (matches.Count == 0)
            return Response(request, AiFunctionStatus.NotFound, "No exact active menu item match was found.");
        if (matches.Count > 1)
            return Response(request, AiFunctionStatus.Ambiguous, "More than one active menu item has that name.");

        var item = matches[0];
        var fact = new AiMenuItemFact(
            item.Id,
            item.Name,
            item.Category,
            item.Description,
            roles.Contains(RomsRoles.Kitchen) && !roles.Contains(RomsRoles.Admin) ? null : item.Price,
            "PHP",
            item.IsAvailable);
        var priceText = fact.Price is null ? "" : $" The stored price is PHP {fact.Price:0.00}.";
        return Response(request, AiFunctionStatus.Success,
            $"{fact.Name} is {(fact.Available ? "available" : "unavailable")}.{priceText}", fact);
    }

    private static async Task<AiFunctionResponse> ListMenuAsync(
        RomsDbContext db,
        AiFunctionRequest request,
        IReadOnlySet<string> roles,
        CancellationToken ct)
    {
        if (!CanUseAssistant(roles))
            return Response(request, AiFunctionStatus.Unauthorized,
                "You do not have permission to view menu information.");
        var query = db.MenuItems.AsNoTracking()
            .Where(x => x.IsActive && x.Category!.IsActive);
        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            var category = request.Category.Trim();
            query = query.Where(x => x.Category!.Name.ToUpper() == category.ToUpper());
        }
        if (request.Available is not null)
            query = query.Where(x => x.IsAvailable == request.Available.Value);

        var hidePrice = roles.Contains(RomsRoles.Kitchen) && !roles.Contains(RomsRoles.Admin);
        var items = await query
            .OrderBy(x => x.Category!.SortOrder)
            .ThenBy(x => x.Name)
            .Take(AiFunctionProtocol.MaximumResults)
            .Select(x => new AiMenuItemFact(
                x.Id,
                x.Name,
                x.Category!.Name,
                x.Description,
                hidePrice ? null : x.Price,
                "PHP",
                x.IsAvailable))
            .ToListAsync(ct);

        return Response(request, AiFunctionStatus.Success,
            $"Found {items.Count} active menu item(s).", items);
    }

    private static async Task<AiFunctionResponse> GetInventoryBalanceAsync(
        RomsDbContext db,
        AiFunctionRequest request,
        IReadOnlySet<string> roles,
        CancellationToken ct)
    {
        if (!CanReadInventory(roles))
            return Response(request, AiFunctionStatus.Unauthorized,
                "You do not have permission to view inventory balances.");
        if (string.IsNullOrWhiteSpace(request.ItemName))
            return Response(request, AiFunctionStatus.InvalidRequest, "An inventory item name is required.");

        var name = request.ItemName.Trim();
        var matches = await InventoryQuery(ActiveInventoryItems(db)
                .Where(x => x.Name.ToUpper() == name.ToUpper())
                .OrderBy(x => x.Name)
                .Take(2))
            .ToListAsync(ct);
        if (matches.Count == 0)
            return Response(request, AiFunctionStatus.NotFound, "No exact active inventory item match was found.");
        if (matches.Count > 1)
            return Response(request, AiFunctionStatus.Ambiguous, "More than one active inventory item has that name.");

        var fact = matches[0];
        return Response(request, AiFunctionStatus.Success,
            $"{fact.Name} has {fact.CurrentStock:0.###} {fact.Unit} recorded and is {(fact.IsLowStock ? "at or below" : "above")} its minimum level.",
            fact);
    }

    private static async Task<AiFunctionResponse> ListInventoryAsync(
        RomsDbContext db,
        AiFunctionRequest request,
        IReadOnlySet<string> roles,
        bool lowStockOnly,
        CancellationToken ct)
    {
        if (!CanReadInventory(roles))
            return Response(request, AiFunctionStatus.Unauthorized,
                "You do not have permission to view inventory balances.");

        var source = ActiveInventoryItems(db);
        if (lowStockOnly)
            source = source.Where(x => x.Movements.Sum(movement => movement.QuantityDelta) <= x.MinimumStock);
        var items = await InventoryQuery(source.OrderBy(x => x.Name)
                .Take(AiFunctionProtocol.MaximumResults))
            .ToListAsync(ct);
        return Response(request, AiFunctionStatus.Success,
            $"Found {items.Count} {(lowStockOnly ? "low-stock " : "")}inventory item(s).", items);
    }

    private static async Task<AiFunctionResponse> GetOrderStatusAsync(
        RomsDbContext db,
        AiFunctionRequest request,
        string actor,
        IReadOnlySet<string> roles,
        CancellationToken ct)
    {
        if ((request.OrderId is null) == string.IsNullOrWhiteSpace(request.TableNumber))
            return Response(request, AiFunctionStatus.InvalidRequest,
                "Provide exactly one order ID or table number.");

        var query = OrderQuery(db);
        if (request.OrderId is not null)
            query = query.Where(x => x.Id == request.OrderId.Value);
        else
        {
            var table = request.TableNumber!.Trim();
            query = query.Where(x => x.Table!.Number.ToUpper() == table.ToUpper() &&
                                     (x.Status == OrderStatus.Draft ||
                                      x.Status == OrderStatus.New ||
                                      x.Status == OrderStatus.Preparing ||
                                      x.Status == OrderStatus.Ready ||
                                      (x.Status == OrderStatus.Completed && x.PaymentConfirmedUtc == null)))
                .OrderByDescending(x => x.UpdatedUtc);
        }

        var order = await query.FirstOrDefaultAsync(ct);
        if (order is null)
            return Response(request, AiFunctionStatus.NotFound, "No matching order was found.");
        if (!CanReadOrder(order, actor, roles))
            return Response(request, AiFunctionStatus.Unauthorized,
                "You do not have permission to view that order.");

        var fact = MapOrder(order, roles);
        return Response(request, AiFunctionStatus.Success,
            $"Table {fact.TableNumber} is currently {fact.Status}.", fact);
    }

    private static async Task<AiFunctionResponse> ListOrdersByStatusAsync(
        RomsDbContext db,
        AiFunctionRequest request,
        string actor,
        IReadOnlySet<string> roles,
        CancellationToken ct)
    {
        if (request.OrderStatus is null)
            return Response(request, AiFunctionStatus.InvalidRequest, "An order status is required.");
        if (roles.Contains(RomsRoles.Kitchen) && !roles.Contains(RomsRoles.Admin) &&
            !KitchenStatuses.Contains(request.OrderStatus.Value))
            return Response(request, AiFunctionStatus.Unauthorized,
                "Kitchen staff may view only New, Preparing, and Ready orders.");

        var query = OrderQuery(db).Where(x => x.Status == request.OrderStatus.Value);
        if (roles.Contains(RomsRoles.Waiter) && !roles.Contains(RomsRoles.Admin))
        {
            if (request.OrderStatus == OrderStatus.Cancelled)
                return Response(request, AiFunctionStatus.Unauthorized,
                    "Waiters may not list cancelled-order history.");
            if (request.OrderStatus == OrderStatus.Completed)
                query = query.Where(x => x.PaymentConfirmedUtc == null);
            query = query.Where(x => x.WaiterId == actor);
        }
        else if (!roles.Contains(RomsRoles.Admin) && !roles.Contains(RomsRoles.Kitchen))
            return Response(request, AiFunctionStatus.Unauthorized,
                "You do not have permission to list orders.");

        var orders = await query.OrderBy(x => x.UpdatedUtc)
            .Take(AiFunctionProtocol.MaximumResults)
            .ToListAsync(ct);
        var facts = orders.Select(x => MapOrder(x, roles)).ToList();
        return Response(request, AiFunctionStatus.Success,
            $"Found {facts.Count} permitted {request.OrderStatus} order(s).", facts);
    }

    private async Task<AiFunctionResponse> GetDailyOrderSummaryAsync(
        RomsDbContext db,
        AiFunctionRequest request,
        IReadOnlySet<string> roles,
        CancellationToken ct)
    {
        if (!roles.Contains(RomsRoles.Admin))
            return Response(request, AiFunctionStatus.Unauthorized,
                "Only an administrator may view completed-order values.");
        var date = request.BusinessDate ?? LocalDate(clock.UtcNow);
        var fact = await BuildDailySummaryAsync(db, date, ct);
        return Response(request, AiFunctionStatus.Success,
            $"On {date:yyyy-MM-dd}, {fact.PaidCompletedOrders} paid completed order(s) recorded PHP {fact.PaidCompletedOrderValue:0.00}; {fact.CancelledOrders} order(s) were cancelled.",
            fact);
    }

    private static async Task<AiFunctionResponse> GetOrderStatusSummaryAsync(
        RomsDbContext db,
        AiFunctionRequest request,
        IReadOnlySet<string> roles,
        CancellationToken ct)
    {
        if (!roles.Contains(RomsRoles.Admin))
            return Response(request, AiFunctionStatus.Unauthorized,
                "Only an administrator may view the full order-status summary.");
        var fact = await BuildOrderStatusSummaryAsync(db, ct);
        return Response(request, AiFunctionStatus.Success,
            $"Active orders: {fact.Draft} Draft, {fact.New} New, {fact.Preparing} Preparing, {fact.Ready} Ready, and {fact.PendingPayment} awaiting payment.",
            fact);
    }

    private static async Task<AiFunctionResponse> GetLowStockSummaryAsync(
        RomsDbContext db,
        AiFunctionRequest request,
        IReadOnlySet<string> roles,
        CancellationToken ct)
    {
        if (!CanReadInventory(roles))
            return Response(request, AiFunctionStatus.Unauthorized,
                "You do not have permission to view low-stock information.");
        var items = await InventoryQuery(ActiveInventoryItems(db)
                .Where(x => x.Movements.Sum(movement => movement.QuantityDelta) <= x.MinimumStock)
                .OrderBy(x => x.Name)
                .Take(AiFunctionProtocol.MaximumResults))
            .ToListAsync(ct);
        var fact = new AiLowStockSummaryFact(items.Count, items);
        return Response(request, AiFunctionStatus.Success,
            $"{fact.LowStockCount} inventory item(s) are at or below their minimum levels.", fact);
    }

    private static async Task<AiFunctionResponse> GetMenuAvailabilitySummaryAsync(
        RomsDbContext db,
        AiFunctionRequest request,
        IReadOnlySet<string> roles,
        CancellationToken ct)
    {
        if (!CanUseAssistant(roles))
            return Response(request, AiFunctionStatus.Unauthorized, "You do not have permission to view menu information.");
        var query = db.MenuItems.AsNoTracking().Where(x => x.IsActive && x.Category!.IsActive);
        var active = await query.CountAsync(ct);
        var available = await query.CountAsync(x => x.IsAvailable, ct);
        var fact = new AiMenuAvailabilitySummaryFact(active, available, active - available);
        return Response(request, AiFunctionStatus.Success,
            $"{fact.AvailableItems} of {fact.ActiveItems} active menu item(s) are available; {fact.UnavailableItems} are unavailable.",
            fact);
    }

    private async Task<AiFunctionResponse> GetOperationalSummaryAsync(
        RomsDbContext db,
        AiFunctionRequest request,
        IReadOnlySet<string> roles,
        CancellationToken ct)
    {
        if (!roles.Contains(RomsRoles.Admin))
            return Response(request, AiFunctionStatus.Unauthorized,
                "Only an administrator may view the full operational summary.");
        var date = request.BusinessDate ?? LocalDate(clock.UtcNow);
        var daily = await BuildDailySummaryAsync(db, date, ct);
        var status = await BuildOrderStatusSummaryAsync(db, ct);
        var lowStock = await ActiveInventoryItems(db)
            .CountAsync(x => x.Movements.Sum(movement => movement.QuantityDelta) <= x.MinimumStock, ct);
        var unavailable = await db.MenuItems.AsNoTracking()
            .CountAsync(x => x.IsActive && x.Category!.IsActive && !x.IsAvailable, ct);
        var fact = new AiOperationalSummaryFact(
            date,
            AiFunctionProtocol.RestaurantTimeZone,
            status.Draft + status.New + status.Preparing + status.Ready + status.PendingPayment,
            status.Ready,
            daily.PaidCompletedOrders,
            daily.PaidCompletedOrderValue,
            lowStock,
            unavailable,
            "PHP");
        return Response(request, AiFunctionStatus.Success,
            $"There are {fact.ActiveOrders} active order(s), including {fact.ReadyOrders} ready. " +
            $"{fact.PaidCompletedOrders} paid order(s) recorded PHP {fact.PaidCompletedOrderValue:0.00} on {date:yyyy-MM-dd}. " +
            $"{fact.LowStockItems} inventory item(s) are low and {fact.UnavailableMenuItems} active menu item(s) are unavailable.",
            fact);
    }

    private async Task<AiDailyOrderSummaryFact> BuildDailySummaryAsync(
        RomsDbContext db,
        DateOnly date,
        CancellationToken ct)
    {
        var (fromUtc, toUtc) = BusinessDayUtcRange(date);
        var paidOrders = await db.Orders.AsNoTracking().Include(x => x.Items)
            .Where(x => x.Status == OrderStatus.Completed &&
                        x.PaymentConfirmedUtc >= fromUtc && x.PaymentConfirmedUtc < toUtc)
            .ToListAsync(ct);
        var cancellations = await db.Set<OrderStatusHistory>().AsNoTracking()
            .CountAsync(x => x.ToStatus == OrderStatus.Cancelled &&
                             x.OccurredUtc >= fromUtc && x.OccurredUtc < toUtc, ct);
        return new AiDailyOrderSummaryFact(
            date,
            AiFunctionProtocol.RestaurantTimeZone,
            paidOrders.Count,
            cancellations,
            paidOrders.Sum(x => x.Total),
            "PHP");
    }

    private static async Task<AiOrderStatusSummaryFact> BuildOrderStatusSummaryAsync(
        RomsDbContext db,
        CancellationToken ct)
    {
        var counts = await db.Orders.AsNoTracking()
            .GroupBy(x => x.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, ct);
        var pendingPayment = await db.Orders.AsNoTracking()
            .CountAsync(x => x.Status == OrderStatus.Completed && x.PaymentConfirmedUtc == null, ct);
        return new AiOrderStatusSummaryFact(
            counts.GetValueOrDefault(OrderStatus.Draft),
            counts.GetValueOrDefault(OrderStatus.New),
            counts.GetValueOrDefault(OrderStatus.Preparing),
            counts.GetValueOrDefault(OrderStatus.Ready),
            pendingPayment);
    }

    private static IQueryable<InventoryItem> ActiveInventoryItems(RomsDbContext db) =>
        db.InventoryItems.AsNoTracking().Where(x => x.IsActive);

    private static IQueryable<AiInventoryFact> InventoryQuery(IQueryable<InventoryItem> query) =>
        query
            .Select(x => new AiInventoryFact(
                x.Id,
                x.Name,
                x.Movements.Sum(movement => movement.QuantityDelta),
                x.Unit,
                x.MinimumStock,
                x.Movements.Sum(movement => movement.QuantityDelta) <= x.MinimumStock));

    private static IQueryable<Order> OrderQuery(RomsDbContext db) =>
        db.Orders.AsNoTracking()
            .Include(x => x.Table)
            .Include(x => x.Items);

    private static AiOrderStatusFact MapOrder(Order order, IReadOnlySet<string> roles)
    {
        var kitchenOnly = roles.Contains(RomsRoles.Kitchen) && !roles.Contains(RomsRoles.Admin);
        return new AiOrderStatusFact(
            order.Id,
            order.Table?.Number ?? "?",
            order.Status,
            order.CreatedUtc,
            order.SubmittedUtc,
            order.CompletedUtc,
            order.UpdatedUtc,
            order.PaymentConfirmedUtc is not null,
            kitchenOnly ? null : order.Total,
            order.Items.Where(x => !x.IsRemoved)
                .OrderBy(x => x.MenuItemName)
                .Select(x => new AiOrderItemFact(x.MenuItemName, x.Quantity, kitchenOnly ? null : x.UnitPrice))
                .ToList());
    }

    private static bool CanReadOrder(Order order, string actor, IReadOnlySet<string> roles) =>
        roles.Contains(RomsRoles.Admin) ||
        (roles.Contains(RomsRoles.Waiter) && string.Equals(order.WaiterId, actor, StringComparison.OrdinalIgnoreCase)) ||
        (roles.Contains(RomsRoles.Kitchen) && KitchenStatuses.Contains(order.Status));

    private static bool CanReadInventory(IReadOnlySet<string> roles) =>
        roles.Contains(RomsRoles.Admin) || roles.Contains(RomsRoles.Kitchen);

    private static bool CanUseAssistant(IReadOnlySet<string> roles) =>
        roles.Contains(RomsRoles.Admin) || roles.Contains(RomsRoles.Waiter) || roles.Contains(RomsRoles.Kitchen);

    private static async Task<HashSet<string>> GetRolesAsync(
        RomsDbContext db,
        string username,
        CancellationToken ct) =>
        (await (from user in db.Users.AsNoTracking()
                join userRole in db.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
                join role in db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                where user.UserName == username
                select role.Name!)
            .ToListAsync(ct))
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private async Task<AiFunctionResponse> FinishAsync(
        RomsDbContext db,
        AiFunctionRequest request,
        string actor,
        AiFunctionResponse response,
        CancellationToken ct)
    {
        db.AuditEntries.Add(new AuditEntry
        {
            ActorId = actor,
            Action = $"AiRead:{request.Function}",
            EntityType = "AiFunction",
            EntityId = request.Function.ToString(),
            NewValuesJson = JsonSerializer.Serialize(new
            {
                response.Status,
                Arguments = new
                {
                    request.ItemName,
                    request.Category,
                    request.Available,
                    request.OrderId,
                    request.TableNumber,
                    request.OrderStatus,
                    request.BusinessDate
                }
            }),
            OccurredUtc = clock.UtcNow
        });
        await db.SaveChangesAsync(ct);
        return response;
    }

    private static AiFunctionResponse Response(
        AiFunctionRequest request,
        AiFunctionStatus status,
        string message,
        object? data = null) =>
        new(AiFunctionProtocol.CurrentVersion, request.Function, status, message, data);

    private static DateOnly LocalDate(DateTime utcNow) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(utcNow, DateTimeKind.Utc),
            RestaurantTimeZone()));

    private static (DateTime FromUtc, DateTime ToUtc) BusinessDayUtcRange(DateOnly date)
    {
        var localStart = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var localEnd = date.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var zone = RestaurantTimeZone();
        return (TimeZoneInfo.ConvertTimeToUtc(localStart, zone), TimeZoneInfo.ConvertTimeToUtc(localEnd, zone));
    }

    private static TimeZoneInfo RestaurantTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(AiFunctionProtocol.RestaurantTimeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
        }
    }
}
