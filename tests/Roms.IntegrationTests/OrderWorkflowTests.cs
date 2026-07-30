using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Roms.Application;
using Roms.Domain;
using Roms.Infrastructure.Persistence;
using Roms.Infrastructure.Services;
using Roms.Infrastructure.Identity;

namespace Roms.IntegrationTests;

public sealed class OrderWorkflowTests : IAsyncLifetime
{
    private DbContextOptions<RomsDbContext> options = default!;

    public async Task InitializeAsync()
    {
        options = new DbContextOptionsBuilder<RomsDbContext>()
            .UseInMemoryDatabase($"roms-tests-{Guid.NewGuid()}")
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options;
        await using var db = new RomsDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var category = new MenuCategory { Name = "Mains" };
        var item = new MenuItem { Name = "Burger", Price = 185m };
        category.Items.Add(item);
        db.RestaurantTables.Add(new RestaurantTable { Number = "1" });
        db.MenuCategories.Add(category);
        var kitchenRole = new IdentityRole(RomsRoles.Kitchen) { NormalizedName = RomsRoles.Kitchen.ToUpperInvariant() };
        var adminRole = new IdentityRole(RomsRoles.Admin) { NormalizedName = RomsRoles.Admin.ToUpperInvariant() };
        var waiter = new ApplicationUser { UserName = "waiter", NormalizedUserName = "WAITER", DisplayName = "Waiter One" };
        var waiter2 = new ApplicationUser { UserName = "waiter2", NormalizedUserName = "WAITER2", DisplayName = "Waiter Two" };
        var kitchen = new ApplicationUser { UserName = "kitchen", NormalizedUserName = "KITCHEN", DisplayName = "Kitchen One" };
        var admin = new ApplicationUser { UserName = "admin", NormalizedUserName = "ADMIN", DisplayName = "Administrator" };
        db.Roles.AddRange(kitchenRole, adminRole);
        db.Users.AddRange(waiter, waiter2, kitchen, admin);
        db.UserRoles.AddRange(
            new IdentityUserRole<string> { UserId = kitchen.Id, RoleId = kitchenRole.Id },
            new IdentityUserRole<string> { UserId = admin.Id, RoleId = adminRole.Id });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Duplicate_submission_key_returns_the_same_order()
    {
        await using var db = new RomsDbContext(options);
        var table = await db.RestaurantTables.SingleAsync();
        var menu = await db.MenuItems.SingleAsync();
        var service = CreateService();
        var id = await service.GetOrCreateDraftAsync(table.Id, "waiter");
        await service.AddItemAsync(id, menu.Id, 2, null, "waiter");
        var first = await service.SubmitAsync(id, "submit-1", "waiter");
        var second = await service.SubmitAsync(id, "submit-1", "waiter");
        Assert.Equal(first, second);
        Assert.Equal(1, await db.IdempotencyRecords.CountAsync());
        Assert.Equal("Waiter One", (await service.GetKitchenOrdersAsync()).Single().WaiterName);
    }

    [Fact]
    public async Task Active_table_is_locked_to_its_waiter_but_admin_can_cancel()
    {
        await using var db = new RomsDbContext(options);
        var table = await db.RestaurantTables.SingleAsync();
        var service = CreateService();
        var id = await service.GetOrCreateDraftAsync(table.Id, "waiter");

        var card = (await service.GetTablesAsync()).Single();
        Assert.Equal("Waiter One", card.WaiterName);
        await Assert.ThrowsAsync<DomainException>(() => service.GetOrCreateDraftAsync(table.Id, "waiter2"));
        await Assert.ThrowsAsync<DomainException>(() => service.GetOrderAsync(id, "waiter2"));
        await Assert.ThrowsAsync<DomainException>(() => service.TransitionAsync(id, OrderStatus.Cancelled, "waiter2", "Not my table"));

        await service.TransitionAsync(id, OrderStatus.Cancelled, "admin", "Admin override");
        Assert.Equal(OrderStatus.Cancelled, (await db.Orders.SingleAsync()).Status);
    }

    [Fact]
    public async Task Entering_preparing_posts_recipe_consumption_once()
    {
        await using (var setup = new RomsDbContext(options))
        {
            var menu = await setup.MenuItems.SingleAsync();
            var stock = new InventoryItem { Name = "Patty", Unit = "piece" };
            menu.RecipeIngredients.Add(new RecipeIngredient { InventoryItem = stock, Quantity = 1m });
            await setup.SaveChangesAsync();
        }
        await using var db = new RomsDbContext(options);
        var service = CreateService(inventory: true);
        var table = await db.RestaurantTables.SingleAsync(); var menuItem = await db.MenuItems.SingleAsync();
        var id = await service.GetOrCreateDraftAsync(table.Id, "waiter");
        await service.AddItemAsync(id, menuItem.Id, 2, null, "waiter");
        await service.SubmitAsync(id, "submit-stock", "waiter");
        await service.TransitionAsync(id, OrderStatus.Preparing, "kitchen");
        var movement = await db.StockMovements.SingleAsync();
        Assert.Equal(-2m, movement.QuantityDelta);
        Assert.Equal($"order:{id}:preparing:{movement.InventoryItemId}", movement.IdempotencyKey);
    }

    [Fact]
    public async Task Cancelling_a_prepared_order_reverses_its_inventory_consumption()
    {
        await using (var setup = new RomsDbContext(options))
        {
            var menu = await setup.MenuItems.SingleAsync();
            menu.RecipeIngredients.Add(new RecipeIngredient
                { InventoryItem = new InventoryItem { Name = "Patty", Unit = "piece" }, Quantity = 1m });
            await setup.SaveChangesAsync();
        }
        await using var db = new RomsDbContext(options);
        var service = CreateService(inventory: true);
        var table = await db.RestaurantTables.SingleAsync();
        var menuItem = await db.MenuItems.SingleAsync();
        var id = await service.GetOrCreateDraftAsync(table.Id, "waiter");
        await service.AddItemAsync(id, menuItem.Id, 2, null, "waiter");
        await service.SubmitAsync(id, "cancel-stock", "waiter");
        await service.TransitionAsync(id, OrderStatus.Preparing, "kitchen");
        await service.TransitionAsync(
            id,
            OrderStatus.Cancelled,
            "admin",
            "Customer left",
            inventoryDisposition: InventoryDisposition.ReturnToStock);

        var movements = await db.StockMovements.OrderBy(x => x.Id).ToListAsync();
        Assert.Equal(2, movements.Count);
        Assert.Equal(StockMovementType.Consumption, movements[0].Type);
        Assert.Equal(StockMovementType.Reversal, movements[1].Type);
        Assert.Equal(0m, movements.Sum(x => x.QuantityDelta));
    }

    [Fact]
    public async Task Cancelling_a_prepared_order_as_waste_preserves_consumption_and_audits_disposition()
    {
        await using (var setup = new RomsDbContext(options))
        {
            var menu = await setup.MenuItems.SingleAsync();
            menu.RecipeIngredients.Add(new RecipeIngredient
                { InventoryItem = new InventoryItem { Name = "Patty", Unit = "piece" }, Quantity = 1m });
            await setup.SaveChangesAsync();
        }
        await using var db = new RomsDbContext(options);
        var service = CreateService(inventory: true);
        var table = await db.RestaurantTables.SingleAsync();
        var menuItem = await db.MenuItems.SingleAsync();
        var id = await service.GetOrCreateDraftAsync(table.Id, "waiter");
        await service.AddItemAsync(id, menuItem.Id, 2, null, "waiter");
        await service.SubmitAsync(id, "waste-stock", "waiter");
        await service.TransitionAsync(id, OrderStatus.Preparing, "kitchen");
        await service.TransitionAsync(
            id,
            OrderStatus.Cancelled,
            "admin",
            "Converted to staff meal",
            inventoryDisposition: InventoryDisposition.ConsumedAsWasteOrStaffMeal);

        var order = await db.Orders.SingleAsync(x => x.Id == id);
        var movements = await db.StockMovements.ToListAsync();
        var audit = await db.AuditEntries.OrderByDescending(x => x.Id)
            .FirstAsync(x => x.Action == "CancelOrder");

        Assert.Equal(-2m, movements.Sum(x => x.QuantityDelta));
        Assert.Single(movements);
        Assert.Equal(
            InventoryDisposition.ConsumedAsWasteOrStaffMeal,
            order.CancellationInventoryDisposition);
        Assert.Contains("ConsumedAsWasteOrStaffMeal", audit.NewValuesJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Preparing_order_amendments_reconcile_inventory_to_active_items()
    {
        await using (var setup = new RomsDbContext(options))
        {
            var menu = await setup.MenuItems.SingleAsync();
            menu.RecipeIngredients.Add(new RecipeIngredient
                { InventoryItem = new InventoryItem { Name = "Patty", Unit = "piece" }, Quantity = 1m });
            await setup.SaveChangesAsync();
        }
        await using var db = new RomsDbContext(options);
        var service = CreateService(inventory: true);
        var table = await db.RestaurantTables.SingleAsync();
        var menuItem = await db.MenuItems.SingleAsync();
        var id = await service.GetOrCreateDraftAsync(table.Id, "waiter");
        await service.AddItemAsync(id, menuItem.Id, 2, null, "waiter");
        await service.SubmitAsync(id, "amend-stock", "waiter");
        await service.TransitionAsync(id, OrderStatus.Preparing, "kitchen");

        await service.AmendAddItemAsync(id, menuItem.Id, 1, null, "Extra burger", "waiter");
        var addedItemId = (await db.Orders.Include(x => x.Items).SingleAsync(x => x.Id == id))
            .Items.Single(x => x.Quantity == 1).Id;
        Assert.Equal(-3m, (await db.StockMovements.ToListAsync()).Sum(x => x.QuantityDelta));

        await service.AmendRemoveItemAsync(
            id,
            addedItemId,
            "Admin-approved correction",
            "admin",
            inventoryDisposition: InventoryDisposition.ReturnToStock);
        var movements = await db.StockMovements.OrderBy(x => x.Id).ToListAsync();
        Assert.Equal(3, movements.Count);
        Assert.Equal(-2m, movements.Sum(x => x.QuantityDelta));
        Assert.Equal(StockMovementType.Reversal, movements[^1].Type);
    }

    [Fact]
    public async Task Prepared_item_removed_as_waste_stays_consumed_after_a_later_amendment()
    {
        await using (var setup = new RomsDbContext(options))
        {
            var menu = await setup.MenuItems.SingleAsync();
            menu.RecipeIngredients.Add(new RecipeIngredient
                { InventoryItem = new InventoryItem { Name = "Patty", Unit = "piece" }, Quantity = 1m });
            await setup.SaveChangesAsync();
        }
        await using var db = new RomsDbContext(options);
        var service = CreateService(inventory: true);
        var table = await db.RestaurantTables.SingleAsync();
        var menuItem = await db.MenuItems.SingleAsync();
        var id = await service.GetOrCreateDraftAsync(table.Id, "waiter");
        await service.AddItemAsync(id, menuItem.Id, 1, null, "waiter");
        await service.SubmitAsync(id, "waste-amendment", "waiter");
        await service.TransitionAsync(id, OrderStatus.Preparing, "kitchen");

        var originalItemId = (await db.Orders.Include(x => x.Items).SingleAsync(x => x.Id == id))
            .Items.Single().Id;
        await service.AmendRemoveItemAsync(
            id,
            originalItemId,
            "Dish dropped after preparation",
            "admin",
            inventoryDisposition: InventoryDisposition.ConsumedAsWasteOrStaffMeal);
        await service.AmendAddItemAsync(
            id,
            menuItem.Id,
            1,
            null,
            "Replacement dish",
            "waiter");

        db.ChangeTracker.Clear();
        var order = await db.Orders.Include(x => x.Items).SingleAsync(x => x.Id == id);
        var movements = await db.StockMovements.OrderBy(x => x.Id).ToListAsync();

        Assert.Equal(-2m, movements.Sum(x => x.QuantityDelta));
        Assert.Equal(
            InventoryDisposition.ConsumedAsWasteOrStaffMeal,
            order.Items.Single(x => x.Id == originalItemId).RemovalInventoryDisposition);
        Assert.All(movements, movement => Assert.Equal(StockMovementType.Consumption, movement.Type));
    }

    [Fact]
    public async Task Kitchen_and_waiter_complete_the_full_serving_workflow()
    {
        await using var db = new RomsDbContext(options);
        var table = await db.RestaurantTables.SingleAsync(); var menu = await db.MenuItems.SingleAsync();
        var service = CreateService();
        var id = await service.GetOrCreateDraftAsync(table.Id, "waiter");
        await service.AddItemAsync(id, menu.Id, 1, null, "waiter");
        await service.SubmitAsync(id, "full-flow", "waiter");

        await service.TransitionAsync(id, OrderStatus.Preparing, "kitchen");
        Assert.Equal(TableStatus.Preparing, (await service.GetTablesAsync()).Single().Status);
        await Assert.ThrowsAsync<DomainException>(() => service.TransitionAsync(id, OrderStatus.Cancelled, "waiter", "Too late"));
        await service.TransitionAsync(id, OrderStatus.Ready, "kitchen");
        Assert.Equal(TableStatus.ReadyToServe, (await service.GetTablesAsync()).Single().Status);
        await service.TransitionAsync(id, OrderStatus.Completed, "waiter");
        Assert.Equal(TableStatus.PendingPayment, (await service.GetTablesAsync()).Single().Status);
        Assert.Equal(OrderStatus.Completed, (await db.Orders.SingleAsync()).Status);
        Assert.Single(await service.GetPendingPaymentsAsync());

        var reports = new ReportService(new TestFactory(options));
        var beforePayment = await reports.GetDashboardAsync(new(2026,7,13,0,0,0,DateTimeKind.Utc), new(2026,7,14,0,0,0,DateTimeKind.Utc));
        Assert.Equal(0, beforePayment.OrderCount);
        await Assert.ThrowsAsync<DomainException>(() => service.ConfirmPaymentAsync(id, "waiter"));
        await service.ConfirmPaymentAsync(id, "admin");
        Assert.Equal(TableStatus.Available, (await service.GetTablesAsync()).Single().Status);
        Assert.Empty(await service.GetPendingPaymentsAsync());
        var afterPayment = await reports.GetDashboardAsync(new(2026,7,13,0,0,0,DateTimeKind.Utc), new(2026,7,14,0,0,0,DateTimeKind.Utc));
        Assert.Equal(1, afterPayment.OrderCount);
        Assert.Equal(1, afterPayment.BestSellers.Single().Quantity);
    }

    [Fact]
    public async Task Admin_can_cancel_after_kitchen_accepts_but_waiter_cannot()
    {
        await using var db = new RomsDbContext(options);
        var table = await db.RestaurantTables.SingleAsync(); var menu = await db.MenuItems.SingleAsync();
        var service = CreateService();
        var id = await service.GetOrCreateDraftAsync(table.Id, "waiter");
        await service.AddItemAsync(id, menu.Id, 1, null, "waiter");
        await service.SubmitAsync(id, "admin-cancel", "waiter");
        await service.TransitionAsync(id, OrderStatus.Preparing, "kitchen");

        await Assert.ThrowsAsync<DomainException>(() => service.TransitionAsync(id, OrderStatus.Cancelled, "waiter", "Customer request"));
        await service.TransitionAsync(
            id,
            OrderStatus.Cancelled,
            "admin",
            "Approved customer request",
            inventoryDisposition: InventoryDisposition.ReturnToStock);
        Assert.Equal(OrderStatus.Cancelled, (await db.Orders.SingleAsync()).Status);
    }

    public Task DisposeAsync() => Task.CompletedTask;
    private OrderService CreateService(bool inventory = false) => new(
        new TestFactory(options),
        new FixedClock(),
        new NoOpPublisher(),
        Options.Create(new InventoryOptions { Enabled = inventory }),
        NullLogger<OrderService>.Instance);

    private sealed class TestFactory(DbContextOptions<RomsDbContext> options) : IDbContextFactory<RomsDbContext>
    { public RomsDbContext CreateDbContext() => new(options); public Task<RomsDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(new RomsDbContext(options)); }
    private sealed class FixedClock : IClock { public DateTime UtcNow => new(2026, 7, 13, 12, 0, 0, DateTimeKind.Utc); }
    private sealed class NoOpPublisher : IOrderEventPublisher { public Task PublishAsync(OrderEvent message, CancellationToken cancellationToken = default) => Task.CompletedTask; }
}
