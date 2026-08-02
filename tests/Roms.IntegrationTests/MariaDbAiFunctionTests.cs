using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Roms.Application;
using Roms.Application.Ai;
using Roms.Domain;
using Roms.Infrastructure.Identity;
using Roms.Infrastructure.Services;

namespace Roms.IntegrationTests;

[Collection(MariaDbCollection.Name)]
public sealed class MariaDbAiFunctionTests(MariaDbFixture fixture)
{
    [Fact]
    public async Task Approved_AI_reads_translate_and_execute_against_real_MariaDb()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var clock = new FixedClock();
        const string adminName = "mariadb-ai-admin";

        await using (var db = database.CreateContext())
        {
            var adminRole = new IdentityRole(RomsRoles.Admin)
            {
                NormalizedName = RomsRoles.Admin.ToUpperInvariant()
            };
            var admin = new ApplicationUser
            {
                UserName = adminName,
                NormalizedUserName = adminName.ToUpperInvariant()
            };
            db.Roles.Add(adminRole);
            db.Users.Add(admin);
            db.UserRoles.Add(new IdentityUserRole<string>
            {
                UserId = admin.Id,
                RoleId = adminRole.Id
            });

            var category = new MenuCategory { Name = "Mains", SortOrder = 1 };
            var menuItem = new MenuItem
            {
                Category = category,
                Name = "MariaDB Burger",
                Description = "Disposable AI query test item",
                Price = 199.50m,
                IsAvailable = true
            };
            db.MenuCategories.Add(category);
            db.MenuItems.Add(menuItem);

            var inventoryItem = new InventoryItem
            {
                Name = "MariaDB Test Oil",
                Unit = "ml",
                MinimumStock = 250.125m
            };
            db.InventoryItems.Add(inventoryItem);
            db.StockMovements.Add(new StockMovement
            {
                InventoryItem = inventoryItem,
                Type = StockMovementType.Receipt,
                QuantityDelta = 200.100m,
                Reason = "Disposable AI query test",
                IdempotencyKey = $"mariadb-ai:{Guid.NewGuid():N}",
                ActorId = adminName,
                OccurredUtc = clock.UtcNow
            });

            var table = new RestaurantTable { Number = "AI-1", SortOrder = 1 };
            var order = new Order
            {
                Table = table,
                WaiterId = adminName,
                CreatedUtc = clock.UtcNow
            };
            order.AddItem(menuItem, 1, null, clock.UtcNow);
            order.Submit(clock.UtcNow);
            order.TransitionTo(OrderStatus.Preparing, adminName, null, clock.UtcNow);
            order.TransitionTo(OrderStatus.Ready, adminName, null, clock.UtcNow);
            order.TransitionTo(OrderStatus.Completed, adminName, null, clock.UtcNow);
            order.ConfirmPayment(adminName, clock.UtcNow);
            db.RestaurantTables.Add(table);
            db.Orders.Add(order);

            await db.SaveChangesAsync();
        }

        var service = new AiFunctionService(database.CreateFactory(), clock);

        var menu = await service.ExecuteAsync(
            new AiFunctionRequest(AiFunctionName.GetMenuItem, ItemName: "mariadb burger"),
            adminName);
        var inventory = await service.ExecuteAsync(
            new AiFunctionRequest(AiFunctionName.ListLowStockItems),
            adminName);
        var summary = await service.ExecuteAsync(
            new AiFunctionRequest(
                AiFunctionName.GetOperationalSummary,
                BusinessDate: new DateOnly(2026, 8, 2)),
            adminName);

        Assert.Equal(AiFunctionStatus.Success, menu.Status);
        Assert.Equal(199.50m, Assert.IsType<AiMenuItemFact>(menu.Data).Price);

        Assert.True(inventory.Status == AiFunctionStatus.Success, inventory.Message);
        var lowStock = Assert.IsAssignableFrom<IReadOnlyList<AiInventoryFact>>(inventory.Data);
        Assert.Single(lowStock);
        Assert.Equal(200.100m, lowStock[0].CurrentStock);

        Assert.True(summary.Status == AiFunctionStatus.Success, summary.Message);
        var operational = Assert.IsType<AiOperationalSummaryFact>(summary.Data);
        Assert.Equal(1, operational.PaidCompletedOrders);
        Assert.Equal(199.50m, operational.PaidCompletedOrderValue);
        Assert.Equal(1, operational.LowStockItems);

        await using var verification = database.CreateContext();
        var audits = await verification.AuditEntries
            .Where(entry => entry.Action.StartsWith("AiRead:"))
            .ToListAsync();
        Assert.Equal(3, audits.Count);
    }

    private sealed class FixedClock : IClock
    {
        public DateTime UtcNow => new(2026, 8, 2, 10, 0, 0, DateTimeKind.Utc);
    }
}
