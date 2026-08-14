using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Roms.Application;
using Roms.Domain;
using Roms.Infrastructure.Identity;
using Roms.Infrastructure.Services;

namespace Roms.IntegrationTests;

[Collection(MariaDbCollection.Name)]
public sealed class MariaDbOrderConcurrencyTests(MariaDbFixture fixture)
{
    [Fact]
    public async Task Simultaneous_preparing_requests_commit_one_transition()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var orderId = await SeedScenarioAsync(database);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var attempts = new[]
        {
            RunAfterSignalAsync(start.Task, () => CreateService(database).TransitionAsync(orderId, OrderStatus.Preparing, "kitchen")),
            RunAfterSignalAsync(start.Task, () => CreateService(database).TransitionAsync(orderId, OrderStatus.Preparing, "kitchen"))
        };
        start.SetResult();
        var results = await Task.WhenAll(attempts);
        Assert.Single(results, result => result is null);
        Assert.Single(results, result => result is not null);

        await using var db = database.CreateContext();
        var order = await db.Orders.Include(x => x.StatusHistory).SingleAsync();
        Assert.Equal(OrderStatus.Preparing, order.Status);
        Assert.Single(order.StatusHistory, x => x.FromStatus == OrderStatus.New && x.ToStatus == OrderStatus.Preparing);
        Assert.Empty(await db.StockMovements.Where(x => x.OrderId == orderId).ToListAsync());
    }

    [Fact]
    public async Task Notification_failure_does_not_undo_a_committed_transition()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var orderId = await SeedScenarioAsync(database);
        await CreateService(database, new ThrowingPublisher())
            .TransitionAsync(orderId, OrderStatus.Preparing, "kitchen");
        await using var db = database.CreateContext();
        Assert.Equal(OrderStatus.Preparing, (await db.Orders.SingleAsync()).Status);
        Assert.Empty(await db.StockMovements.Where(x => x.OrderId == orderId).ToListAsync());
    }

    private static async Task<Exception?> RunAfterSignalAsync(Task start, Func<Task> action)
    {
        await start;
        try { await action(); return null; }
        catch (Exception exception) { return exception; }
    }

    private static async Task<Guid> SeedScenarioAsync(MariaDbTestDatabase database)
    {
        await using var db = database.CreateContext();
        var category = new MenuCategory { Name = "Mains" };
        var menu = new MenuItem { Name = "Burger", Price = 185m };
        category.Items.Add(menu);
        var table = new RestaurantTable { Number = "1" };
        var kitchenRole = new IdentityRole(RomsRoles.Kitchen) { NormalizedName = "KITCHEN" };
        var waiterRole = new IdentityRole(RomsRoles.Waiter) { NormalizedName = "WAITER" };
        var kitchen = new ApplicationUser { UserName = "kitchen", NormalizedUserName = "KITCHEN", DisplayName = "Kitchen" };
        var waiter = new ApplicationUser { UserName = "waiter", NormalizedUserName = "WAITER", DisplayName = "Waiter" };
        db.MenuCategories.Add(category);
        db.RestaurantTables.Add(table);
        db.Roles.AddRange(kitchenRole, waiterRole);
        db.Users.AddRange(kitchen, waiter);
        db.UserRoles.AddRange(
            new IdentityUserRole<string> { UserId = kitchen.Id, RoleId = kitchenRole.Id },
            new IdentityUserRole<string> { UserId = waiter.Id, RoleId = waiterRole.Id });
        db.AttendanceRecords.Add(AttendanceRecord.ClockIn(waiter.Id, null, new FixedClock().UtcNow));
        await db.SaveChangesAsync();
        var service = CreateService(database);
        var orderId = await service.GetOrCreateDraftAsync(table.Id, waiter.UserName!);
        await service.AddItemAsync(orderId, menu.Id, 2, null, waiter.UserName!);
        await service.SubmitAsync(orderId, $"submit:{orderId}", waiter.UserName!);
        return orderId;
    }

    private static OrderService CreateService(MariaDbTestDatabase database, IOrderEventPublisher? publisher = null) =>
        new(database.CreateFactory(), new FixedClock(), publisher ?? new NoOpPublisher(), NullLogger<OrderService>.Instance);

    private sealed class FixedClock : IClock { public DateTime UtcNow => new(2026, 8, 2, 12, 0, 0, DateTimeKind.Utc); }
    private sealed class NoOpPublisher : IOrderEventPublisher { public Task PublishAsync(OrderEvent message, CancellationToken cancellationToken = default) => Task.CompletedTask; }
    private sealed class ThrowingPublisher : IOrderEventPublisher { public Task PublishAsync(OrderEvent message, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Simulated SignalR outage."); }
}
