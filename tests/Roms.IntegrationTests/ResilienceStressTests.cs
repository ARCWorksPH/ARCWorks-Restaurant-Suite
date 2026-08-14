using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Roms.Application;
using Roms.Domain;
using Roms.Infrastructure.Identity;
using Roms.Infrastructure.Services;

namespace Roms.IntegrationTests;

[Collection(MariaDbCollection.Name)]
public sealed class ResilienceStressTests(MariaDbFixture fixture)
{
    [Fact]
    public async Task Sixty_simultaneous_waiter_kitchen_cashier_flows_finish_without_lost_updates()
    {
        const int orderCount = 60;
        await using var database = await fixture.CreateDatabaseAsync();
        var scenario = await SeedAsync(database, orderCount);
        var failures = new ConcurrentBag<Exception>();
        var stopwatch = Stopwatch.StartNew();

        await Parallel.ForEachAsync(scenario.TableIds, new ParallelOptions { MaxDegreeOfParallelism = 12 }, async (tableId, _) =>
        {
            try
            {
                var service = CreateOrderService(database);
                var orderId = await service.GetOrCreateDraftAsync(tableId, scenario.WaiterId);
                await service.AddItemAsync(orderId, scenario.MenuItemId, 1, "<b>allergy text must stay text</b>", scenario.WaiterId);
                await service.SubmitAsync(orderId, $"stress-submit:{orderId}", scenario.WaiterId);
                await service.TransitionAsync(orderId, OrderStatus.Preparing, scenario.KitchenId);
                await service.TransitionAsync(orderId, OrderStatus.Ready, scenario.KitchenId);
                await service.TransitionAsync(orderId, OrderStatus.Completed, scenario.WaiterId);
                await service.ConfirmPaymentAsync(orderId, scenario.AdminId);
            }
            catch (Exception exception) { failures.Add(exception); }
        });
        stopwatch.Stop();

        Assert.Empty(failures);
        await using var db = database.CreateContext();
        Assert.Equal(orderCount, await db.Orders.CountAsync(x => x.Status == OrderStatus.Completed && x.PaymentConfirmedUtc != null));
        Assert.Equal(orderCount * 4, await db.OrderStatusHistory.CountAsync());
        Assert.Equal(orderCount * 7, await db.AuditEntries.CountAsync());
        Assert.Equal(orderCount, await db.IdempotencyRecords.CountAsync());
        Assert.Empty(await db.StockMovements.Where(x => x.OrderId != null).ToListAsync());
        Assert.All(await db.OrderItems.Select(x => x.Notes).ToListAsync(), note => Assert.Equal("<b>allergy text must stay text</b>", note));
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(90), $"The bounded 60-order run took {stopwatch.Elapsed}.");
    }

    private static async Task<Scenario> SeedAsync(MariaDbTestDatabase database, int orderCount)
    {
        await using var db = database.CreateContext();
        var category = new MenuCategory { Name = "Stress menu" };
        var menuItem = new MenuItem { Name = "Synthetic meal", Price = 100m };
        category.Items.Add(menuItem);
        var adminRole = new IdentityRole(RomsRoles.Admin) { NormalizedName = "ADMIN" };
        var kitchenRole = new IdentityRole(RomsRoles.Kitchen) { NormalizedName = "KITCHEN" };
        var waiterRole = new IdentityRole(RomsRoles.Waiter) { NormalizedName = "WAITER" };
        var waiter = new ApplicationUser { UserName = "stress-waiter", NormalizedUserName = "STRESS-WAITER", DisplayName = "Stress Waiter" };
        var kitchen = new ApplicationUser { UserName = "stress-kitchen", NormalizedUserName = "STRESS-KITCHEN", DisplayName = "Stress Kitchen" };
        var admin = new ApplicationUser { UserName = "stress-admin", NormalizedUserName = "STRESS-ADMIN", DisplayName = "Stress Admin" };
        var tables = Enumerable.Range(1, orderCount).Select(x => new RestaurantTable { Number = $"S{x:00}", SortOrder = x }).ToList();
        db.MenuCategories.Add(category);
        db.RestaurantTables.AddRange(tables);
        db.Roles.AddRange(adminRole, kitchenRole, waiterRole);
        db.Users.AddRange(waiter, kitchen, admin);
        db.UserRoles.AddRange(
            new IdentityUserRole<string> { UserId = waiter.Id, RoleId = waiterRole.Id },
            new IdentityUserRole<string> { UserId = kitchen.Id, RoleId = kitchenRole.Id },
            new IdentityUserRole<string> { UserId = admin.Id, RoleId = adminRole.Id });
        db.AttendanceRecords.Add(AttendanceRecord.ClockIn(waiter.Id, null, new FixedClock().UtcNow));
        await db.SaveChangesAsync();
        return new Scenario(tables.Select(x => x.Id).ToList(), menuItem.Id, waiter.UserName!, kitchen.UserName!, admin.UserName!);
    }

    private static OrderService CreateOrderService(MariaDbTestDatabase database) =>
        new(database.CreateFactory(), new FixedClock(), new NoOpPublisher(), NullLogger<OrderService>.Instance);

    private sealed record Scenario(IReadOnlyList<Guid> TableIds, Guid MenuItemId, string WaiterId, string KitchenId, string AdminId);
    private sealed class FixedClock : IClock { public DateTime UtcNow => new(2026, 8, 2, 12, 0, 0, DateTimeKind.Utc); }
    private sealed class NoOpPublisher : IOrderEventPublisher { public Task PublishAsync(OrderEvent message, CancellationToken cancellationToken = default) => Task.CompletedTask; }
}
