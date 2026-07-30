using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Roms.Application;
using Roms.Domain;
using Roms.Infrastructure.Identity;
using Roms.Infrastructure.Services;

namespace Roms.IntegrationTests;

[Collection(MariaDbCollection.Name)]
public sealed class AdversarialInputTests(MariaDbFixture fixture)
{
    [Fact]
    public async Task Invalid_dates_enums_extreme_numbers_and_oversized_text_are_rejected_cleanly()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var scenario = await SeedAsync(database);
        var inventory = new InventoryService(database.CreateFactory(), new FixedClock());
        var reports = new ReportService(database.CreateFactory());
        var attendance = new AttendanceService(database.CreateFactory(), new FixedClock());
        var catalog = new CatalogService(database.CreateFactory(), new FixedClock());

        await Assert.ThrowsAsync<DomainException>(() =>
            reports.GetDashboardAsync(FixedClock.Value, FixedClock.Value.AddDays(-1)));
        await Assert.ThrowsAsync<DomainException>(() =>
            attendance.GetMineAsync("waiter", FixedClock.Value, FixedClock.Value.AddMinutes(-1)));
        await Assert.ThrowsAsync<DomainException>(() =>
            attendance.GetAdminViewAsync(FixedClock.Value, FixedClock.Value));
        await Assert.ThrowsAsync<DomainException>(() =>
            inventory.ReportLossAsync(scenario.InventoryItemId, (InventoryLossType)999, 1m,
                "Invalid enum", scenario.KitchenId, "invalid-enum"));
        await Assert.ThrowsAsync<DomainException>(() =>
            inventory.ReportLossAsync(scenario.InventoryItemId, InventoryLossType.Waste,
                100_000_000_000m, "Too large", scenario.KitchenId, "huge-loss"));
        await Assert.ThrowsAsync<DomainException>(() =>
            inventory.ReportLossAsync(scenario.InventoryItemId, InventoryLossType.Waste, 1m,
                new string('x', 501), scenario.KitchenId, "long-reason"));
        await Assert.ThrowsAsync<DomainException>(() =>
            catalog.SaveTableAsync(new RestaurantTable { Number = new string('9', 21) }, scenario.AdminId));
        await Assert.ThrowsAsync<DomainException>(() =>
            catalog.SaveMenuItemAsync(new MenuItem
            {
                CategoryId = scenario.CategoryId,
                Name = "Impossible price",
                Price = 10_000_000_000m
            }, scenario.AdminId));

        var order = new Order { TableId = scenario.TableId, WaiterId = scenario.WaiterId };
        var menu = new MenuItem { Name = "Meal", Price = 1m };
        Assert.Throws<DomainException>(() => order.AddItem(menu, 0, null, FixedClock.Value));
        Assert.Throws<DomainException>(() => order.AddItem(menu, 100, null, FixedClock.Value));
        Assert.Throws<DomainException>(() =>
            order.AddItem(menu, 1, new string('n', 501), FixedClock.Value));

        await using var db = database.CreateContext();
        Assert.Empty(await db.InventoryLossRequests.ToListAsync());
        Assert.Empty(await db.Orders.ToListAsync());
    }

    [Fact]
    public async Task Sql_and_html_shaped_text_is_stored_as_data_and_duplicate_loss_submission_is_idempotent()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var scenario = await SeedAsync(database);
        var service = new InventoryService(database.CreateFactory(), new FixedClock());
        const string hostileText = "'; DROP TABLE Orders; -- <script>alert(1)</script>";

        var first = await service.ReportLossAsync(
            scenario.InventoryItemId, InventoryLossType.Spoilage, 1m, hostileText,
            scenario.KitchenId, "hostile-but-data");
        var duplicate = await service.ReportLossAsync(
            scenario.InventoryItemId, InventoryLossType.Spoilage, 99m, "different payload",
            scenario.KitchenId, "hostile-but-data");

        Assert.Equal(first, duplicate);
        await using var db = database.CreateContext();
        Assert.True(await db.Database.CanConnectAsync());
        Assert.Single(await db.InventoryLossRequests.ToListAsync());
        Assert.Equal(hostileText, (await db.InventoryLossRequests.SingleAsync()).Reason);
        Assert.True(await db.Orders.CountAsync() == 0);
    }

    private static async Task<Scenario> SeedAsync(MariaDbTestDatabase database)
    {
        await using var db = database.CreateContext();
        var category = new MenuCategory { Name = "Adversarial" };
        var table = new RestaurantTable { Number = "A1" };
        var inventory = new InventoryItem { Name = "Test ingredient", Unit = "piece" };
        var adminRole = new IdentityRole(RomsRoles.Admin)
            { NormalizedName = RomsRoles.Admin.ToUpperInvariant() };
        var kitchenRole = new IdentityRole(RomsRoles.Kitchen)
            { NormalizedName = RomsRoles.Kitchen.ToUpperInvariant() };
        var admin = new ApplicationUser { UserName = "admin", NormalizedUserName = "ADMIN" };
        var kitchen = new ApplicationUser { UserName = "kitchen", NormalizedUserName = "KITCHEN" };
        var waiter = new ApplicationUser { UserName = "waiter", NormalizedUserName = "WAITER" };
        db.MenuCategories.Add(category);
        db.RestaurantTables.Add(table);
        db.InventoryItems.Add(inventory);
        db.Roles.AddRange(adminRole, kitchenRole);
        db.Users.AddRange(admin, kitchen, waiter);
        db.UserRoles.AddRange(
            new IdentityUserRole<string> { UserId = admin.Id, RoleId = adminRole.Id },
            new IdentityUserRole<string> { UserId = kitchen.Id, RoleId = kitchenRole.Id });
        await db.SaveChangesAsync();
        return new(
            category.Id,
            table.Id,
            inventory.Id,
            waiter.UserName!,
            kitchen.UserName!,
            admin.UserName!);
    }

    private sealed record Scenario(
        Guid CategoryId,
        Guid TableId,
        Guid InventoryItemId,
        string WaiterId,
        string KitchenId,
        string AdminId);

    private sealed class FixedClock : IClock
    {
        public static readonly DateTime Value = new(2026, 7, 30, 15, 0, 0, DateTimeKind.Utc);
        public DateTime UtcNow => Value;
    }
}
