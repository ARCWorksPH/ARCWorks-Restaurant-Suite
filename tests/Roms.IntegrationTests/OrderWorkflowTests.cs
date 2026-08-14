using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Roms.Application;
using Roms.Domain;
using Roms.Infrastructure.Identity;
using Roms.Infrastructure.Persistence;
using Roms.Infrastructure.Services;

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
        category.Items.Add(new MenuItem { Name = "Burger", Price = 185m });
        db.RestaurantTables.Add(new RestaurantTable { Number = "1" });
        db.MenuCategories.Add(category);
        var kitchenRole = new IdentityRole(RomsRoles.Kitchen) { NormalizedName = "KITCHEN" };
        var adminRole = new IdentityRole(RomsRoles.Admin) { NormalizedName = "ADMIN" };
        var waiterRole = new IdentityRole(RomsRoles.Waiter) { NormalizedName = "WAITER" };
        var waiter = new ApplicationUser { UserName = "waiter", NormalizedUserName = "WAITER", DisplayName = "Waiter One" };
        var waiter2 = new ApplicationUser { UserName = "waiter2", NormalizedUserName = "WAITER2", DisplayName = "Waiter Two" };
        var kitchen = new ApplicationUser { UserName = "kitchen", NormalizedUserName = "KITCHEN", DisplayName = "Kitchen One" };
        var admin = new ApplicationUser { UserName = "admin", NormalizedUserName = "ADMIN", DisplayName = "Administrator" };
        db.Roles.AddRange(kitchenRole, adminRole, waiterRole);
        db.Users.AddRange(waiter, waiter2, kitchen, admin);
        db.UserRoles.AddRange(
            new IdentityUserRole<string> { UserId = waiter.Id, RoleId = waiterRole.Id },
            new IdentityUserRole<string> { UserId = waiter2.Id, RoleId = waiterRole.Id },
            new IdentityUserRole<string> { UserId = kitchen.Id, RoleId = kitchenRole.Id },
            new IdentityUserRole<string> { UserId = admin.Id, RoleId = adminRole.Id });
        db.AttendanceRecords.Add(AttendanceRecord.ClockIn(waiter.Id, null, new FixedClock().UtcNow));
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Duplicate_submission_key_returns_the_same_order()
    {
        await using var db = new RomsDbContext(options);
        var service = CreateService();
        var id = await service.GetOrCreateDraftAsync((await db.RestaurantTables.SingleAsync()).Id, "waiter");
        await service.AddItemAsync(id, (await db.MenuItems.SingleAsync()).Id, 2, null, "waiter");
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
        var service = CreateService();
        var table = await db.RestaurantTables.SingleAsync();
        var id = await service.GetOrCreateDraftAsync(table.Id, "waiter");
        Assert.Equal("Waiter One", (await service.GetTablesAsync()).Single().WaiterName);
        await Assert.ThrowsAsync<DomainException>(() => service.GetOrCreateDraftAsync(table.Id, "waiter2"));
        await Assert.ThrowsAsync<DomainException>(() => service.GetOrderAsync(id, "waiter2"));
        await service.TransitionAsync(id, OrderStatus.Cancelled, "admin", "Admin override");
        Assert.Equal(OrderStatus.Cancelled, (await db.Orders.SingleAsync()).Status);
    }

    [Fact]
    public async Task Archived_waiter_login_name_still_resolves_historical_display_name()
    {
        await using (var db = new RomsDbContext(options))
        {
            var waiter = await db.Users.SingleAsync(x => x.UserName == "waiter");
            waiter.ArchivedUserName = "waiter";
            waiter.UserName = ApplicationUser.BuildArchivedUserName(waiter.Id, "waiter");
            waiter.NormalizedUserName = waiter.UserName.ToUpperInvariant();
            waiter.IsActive = false;
            await db.SaveChangesAsync();
        }

        var service = CreateService();
        await using var verify = new RomsDbContext(options);
        var tableId = (await verify.RestaurantTables.SingleAsync()).Id;
        var order = new Order
        {
            TableId = tableId,
            WaiterId = "waiter",
            CreatedUtc = new DateTime(2026, 7, 13, 12, 0, 0, DateTimeKind.Utc)
        };
        verify.Orders.Add(order);
        await verify.SaveChangesAsync();

        Assert.Equal("Waiter One", (await service.GetTablesAsync()).Single().WaiterName);
    }

    [Fact]
    public async Task Kitchen_waiter_and_cashier_complete_the_serving_workflow()
    {
        await using var db = new RomsDbContext(options);
        var service = CreateService();
        var id = await service.GetOrCreateDraftAsync((await db.RestaurantTables.SingleAsync()).Id, "waiter");
        await service.AddItemAsync(id, (await db.MenuItems.SingleAsync()).Id, 1, null, "waiter");
        await service.SubmitAsync(id, "full-flow", "waiter");
        await service.TransitionAsync(id, OrderStatus.Preparing, "kitchen");
        await service.TransitionAsync(id, OrderStatus.Ready, "kitchen");
        await service.TransitionAsync(id, OrderStatus.Completed, "waiter");
        Assert.Equal(TableStatus.PendingPayment, (await service.GetTablesAsync()).Single().Status);
        await Assert.ThrowsAsync<DomainException>(() => service.ConfirmPaymentAsync(id, "waiter"));
        await service.ConfirmPaymentAsync(id, "admin");
        Assert.Equal(TableStatus.Available, (await service.GetTablesAsync()).Single().Status);
    }

    [Fact]
    public async Task Kitchen_can_return_order_and_waiter_can_resubmit_with_note()
    {
        await using var db = new RomsDbContext(options);
        var service = CreateService();
        var id = await service.GetOrCreateDraftAsync((await db.RestaurantTables.SingleAsync()).Id, "waiter");
        await service.AddItemAsync(id, (await db.MenuItems.SingleAsync()).Id, 1, null, "waiter");
        await service.SubmitAsync(id, "return-flow-1", "waiter");
        await service.TransitionAsync(id, OrderStatus.ReturnedToWaiter, "kitchen", "Missing side selection");
        await Assert.ThrowsAsync<DomainException>(() => service.SubmitAsync(id, "return-flow-2", "waiter"));
        await service.SubmitAsync(id, "return-flow-2", "waiter", "Confirmed side selection");

        var order = await db.Orders.Include(x => x.StatusHistory).SingleAsync(x => x.Id == id);
        Assert.Equal(OrderStatus.New, order.Status);
        Assert.Equal(1, order.ResubmissionCount);
        Assert.Contains(order.StatusHistory, x => x.ToStatus == OrderStatus.ReturnedToWaiter);
    }

    [Fact]
    public async Task Prepared_order_amendment_and_cancellation_do_not_write_stock_movements()
    {
        await using var db = new RomsDbContext(options);
        var service = CreateService();
        var menu = await db.MenuItems.SingleAsync();
        var id = await service.GetOrCreateDraftAsync((await db.RestaurantTables.SingleAsync()).Id, "waiter");
        await service.AddItemAsync(id, menu.Id, 1, null, "waiter");
        await service.SubmitAsync(id, "no-recipe-stock", "waiter");
        await service.TransitionAsync(id, OrderStatus.Preparing, "kitchen");
        await service.AmendAddItemAsync(id, menu.Id, 1, null, "Extra item", "waiter");
        var itemId = (await db.Orders.Include(x => x.Items).SingleAsync(x => x.Id == id)).Items.Last().Id;
        await service.AmendRemoveItemAsync(id, itemId, "Admin correction", "admin");
        await service.TransitionAsync(id, OrderStatus.Cancelled, "admin", "Customer left");
        Assert.Empty(await db.StockMovements.Where(x => x.OrderId == id).ToListAsync());
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private OrderService CreateService() => new(
        new TestFactory(options), new FixedClock(), new NoOpPublisher(), NullLogger<OrderService>.Instance);

    private sealed class TestFactory(DbContextOptions<RomsDbContext> options) : IDbContextFactory<RomsDbContext>
    { public RomsDbContext CreateDbContext() => new(options); public Task<RomsDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(new RomsDbContext(options)); }
    private sealed class FixedClock : IClock { public DateTime UtcNow => new(2026, 7, 13, 12, 0, 0, DateTimeKind.Utc); }
    private sealed class NoOpPublisher : IOrderEventPublisher { public Task PublishAsync(OrderEvent message, CancellationToken cancellationToken = default) => Task.CompletedTask; }
}
