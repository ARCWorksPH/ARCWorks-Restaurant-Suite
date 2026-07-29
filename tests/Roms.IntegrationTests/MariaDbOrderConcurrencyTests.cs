using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Roms.Application;
using Roms.Domain;
using Roms.Infrastructure.Identity;
using Roms.Infrastructure.Services;

namespace Roms.IntegrationTests;

[Collection(MariaDbCollection.Name)]
public sealed class MariaDbOrderConcurrencyTests(MariaDbFixture fixture)
{
    [Fact]
    public async Task Simultaneous_preparing_requests_consume_inventory_once()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var scenario = await SeedScenarioAsync(database, orderCount: 1);
        var first = CreateService(database);
        var second = CreateService(database);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var attempts = new[]
        {
            TransitionAfterSignalAsync(first, scenario.OrderIds[0], start.Task),
            TransitionAfterSignalAsync(second, scenario.OrderIds[0], start.Task)
        };
        start.SetResult();
        var results = await Task.WhenAll(attempts);

        Assert.Single(results, result => result is null);
        Assert.Single(results, result => result is not null);
        Assert.True(results.Single(result => result is not null) is
            DomainException or DbUpdateConcurrencyException or DbUpdateException);

        await using var db = database.CreateContext();
        var order = await db.Orders.Include(x => x.StatusHistory).SingleAsync();
        var movements = await db.StockMovements.Where(x => x.OrderId == order.Id).ToListAsync();
        Assert.Equal(OrderStatus.Preparing, order.Status);
        Assert.Single(order.StatusHistory, x => x.FromStatus == OrderStatus.New && x.ToStatus == OrderStatus.Preparing);
        Assert.Single(movements);
        Assert.Equal(-2m, movements.Sum(x => x.QuantityDelta));
        Assert.Equal(1, await db.AuditEntries.CountAsync(x => x.Action == "ChangeOrderStatus"));
    }

    [Fact]
    public async Task Separate_orders_can_consume_the_same_inventory_item_concurrently()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var scenario = await SeedScenarioAsync(database, orderCount: 2);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = CreateService(database);
        var second = CreateService(database);

        var attempts = new[]
        {
            TransitionAfterSignalAsync(first, scenario.OrderIds[0], start.Task),
            TransitionAfterSignalAsync(second, scenario.OrderIds[1], start.Task)
        };
        start.SetResult();
        var results = await Task.WhenAll(attempts);

        Assert.All(results, result => Assert.Null(result));
        await using var db = database.CreateContext();
        var movements = await db.StockMovements.OrderBy(x => x.OrderId).ToListAsync();
        Assert.Equal(2, movements.Count);
        Assert.All(movements, movement => Assert.Equal(-2m, movement.QuantityDelta));
        Assert.Equal(-4m, movements.Sum(x => x.QuantityDelta));
        Assert.Equal(2, movements.Select(x => x.IdempotencyKey).Distinct().Count());
    }

    [Fact]
    public async Task Notification_failure_does_not_make_a_committed_transition_look_failed()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var scenario = await SeedScenarioAsync(database, orderCount: 1);
        var service = CreateService(database, new ThrowingPublisher());

        await service.TransitionAsync(scenario.OrderIds[0], OrderStatus.Preparing, "kitchen");

        await using var db = database.CreateContext();
        Assert.Equal(OrderStatus.Preparing, (await db.Orders.SingleAsync()).Status);
        Assert.Equal(-2m, (await db.StockMovements.SingleAsync()).QuantityDelta);
    }

    [Fact]
    public async Task Amendment_racing_with_preparing_keeps_consumption_aligned_with_active_items()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var scenario = await SeedScenarioAsync(database, orderCount: 1);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var amendmentService = CreateService(database);
        var kitchenService = CreateService(database);

        var attempts = new[]
        {
            RunAfterSignalAsync(start.Task, () => amendmentService.AmendAddItemAsync(
                scenario.OrderIds[0],
                scenario.MenuItemId,
                1,
                null,
                "Guest added one item",
                scenario.WaiterId)),
            RunAfterSignalAsync(start.Task, () => kitchenService.TransitionAsync(
                scenario.OrderIds[0],
                OrderStatus.Preparing,
                "kitchen"))
        };
        start.SetResult();
        var results = await Task.WhenAll(attempts);

        Assert.Contains(results, result => result is null);
        Assert.All(results.Where(result => result is not null), result =>
            Assert.True(result is DomainException or DbUpdateConcurrencyException or DbUpdateException));

        await using var db = database.CreateContext();
        var order = await db.Orders.Include(x => x.Items).SingleAsync();
        var activeQuantity = order.Items.Where(x => !x.IsRemoved).Sum(x => x.Quantity);
        var consumedQuantity = -(await db.StockMovements
            .Where(x => x.OrderId == order.Id)
            .SumAsync(x => (decimal?)x.QuantityDelta) ?? 0m);

        Assert.Equal(order.Status == OrderStatus.Preparing ? activeQuantity : 0m, consumedQuantity);
    }

    private static async Task<Exception?> TransitionAfterSignalAsync(
        OrderService service,
        Guid orderId,
        Task start)
        => await RunAfterSignalAsync(
            start,
            () => service.TransitionAsync(orderId, OrderStatus.Preparing, "kitchen"));

    private static async Task<Exception?> RunAfterSignalAsync(
        Task start,
        Func<Task> action)
    {
        await start;
        try
        {
            await action();
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    private static async Task<Scenario> SeedScenarioAsync(MariaDbTestDatabase database, int orderCount)
    {
        await using var db = database.CreateContext();
        var category = new MenuCategory { Name = "Mains" };
        var menu = new MenuItem { Name = "Burger", Price = 185m };
        var inventory = new InventoryItem { Name = "Patty", Unit = "piece" };
        menu.RecipeIngredients.Add(new RecipeIngredient
        {
            InventoryItem = inventory,
            Quantity = 1m
        });
        category.Items.Add(menu);
        var tables = Enumerable.Range(1, orderCount)
            .Select(number => new RestaurantTable { Number = number.ToString() })
            .ToList();
        var kitchenRole = new IdentityRole(RomsRoles.Kitchen)
        {
            NormalizedName = RomsRoles.Kitchen.ToUpperInvariant()
        };
        var waiter = new ApplicationUser
        {
            UserName = "waiter",
            NormalizedUserName = "WAITER",
            DisplayName = "Waiter One"
        };
        var kitchen = new ApplicationUser
        {
            UserName = "kitchen",
            NormalizedUserName = "KITCHEN",
            DisplayName = "Kitchen One"
        };
        db.MenuCategories.Add(category);
        db.RestaurantTables.AddRange(tables);
        db.Roles.Add(kitchenRole);
        db.Users.AddRange(waiter, kitchen);
        db.UserRoles.Add(new IdentityUserRole<string>
        {
            UserId = kitchen.Id,
            RoleId = kitchenRole.Id
        });
        await db.SaveChangesAsync();

        var service = CreateService(database);
        var orderIds = new List<Guid>();
        foreach (var table in tables)
        {
            var orderId = await service.GetOrCreateDraftAsync(table.Id, waiter.Id);
            await service.AddItemAsync(orderId, menu.Id, 2, null, waiter.Id);
            await service.SubmitAsync(orderId, $"submit:{orderId}", waiter.Id);
            orderIds.Add(orderId);
        }
        return new Scenario(orderIds, menu.Id, waiter.Id);
    }

    private static OrderService CreateService(
        MariaDbTestDatabase database,
        IOrderEventPublisher? publisher = null) =>
        new(
            database.CreateFactory(),
            new FixedClock(),
            publisher ?? new NoOpPublisher(),
            Options.Create(new InventoryOptions { Enabled = true }),
            NullLogger<OrderService>.Instance);

    private sealed record Scenario(IReadOnlyList<Guid> OrderIds, Guid MenuItemId, string WaiterId);

    private sealed class FixedClock : IClock
    {
        public DateTime UtcNow => new(2026, 7, 29, 12, 0, 0, DateTimeKind.Utc);
    }

    private sealed class NoOpPublisher : IOrderEventPublisher
    {
        public Task PublishAsync(OrderEvent message, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class ThrowingPublisher : IOrderEventPublisher
    {
        public Task PublishAsync(OrderEvent message, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulated SignalR outage.");
    }
}
