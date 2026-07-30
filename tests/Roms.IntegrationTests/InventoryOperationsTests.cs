using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Roms.Application;
using Roms.Domain;
using Roms.Infrastructure.Identity;
using Roms.Infrastructure.Services;

namespace Roms.IntegrationTests;

[Collection(MariaDbCollection.Name)]
public sealed class InventoryOperationsTests(MariaDbFixture fixture)
{
    [Fact]
    public async Task Receipt_and_physical_count_are_idempotent_audited_and_reconcile_the_ledger()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var scenario = await SeedAsync(database);
        var service = new InventoryService(database.CreateFactory(), new FixedClock());

        await service.ReceiveAsync(
            scenario.ItemId, 10m, "DR-2026-0042", "Ten sealed containers",
            scenario.AdminUsername, "receipt-1");
        await service.ReceiveAsync(
            scenario.ItemId, 99m, "DUPLICATE", "Must not post",
            scenario.AdminUsername, "receipt-1");

        var countId = await service.ReconcileCountAsync(
            scenario.ItemId, 7.5m, "Closing count sheet 42",
            scenario.AdminUsername, "count-1");
        var duplicateId = await service.ReconcileCountAsync(
            scenario.ItemId, 99m, "Duplicate must return original",
            scenario.AdminUsername, "count-1");

        Assert.Equal(countId, duplicateId);
        await using var db = database.CreateContext();
        var movements = await db.StockMovements.OrderBy(x => x.Id).ToListAsync();
        Assert.Collection(
            movements,
            receipt =>
            {
                Assert.Equal(StockMovementType.Receipt, receipt.Type);
                Assert.Equal(10m, receipt.QuantityDelta);
                Assert.Contains("DR-2026-0042", receipt.Reason);
            },
            correction =>
            {
                Assert.Equal(StockMovementType.Adjustment, correction.Type);
                Assert.Equal(-2.5m, correction.QuantityDelta);
                Assert.StartsWith("Physical count:", correction.Reason);
            });
        var count = await db.InventoryCountRecords.SingleAsync();
        Assert.Equal(10m, count.LedgerQuantity);
        Assert.Equal(7.5m, count.CountedQuantity);
        Assert.Equal(-2.5m, count.Variance);
        Assert.Equal(7.5m, await db.StockMovements.SumAsync(x => x.QuantityDelta));
        Assert.Equal(1, await db.AuditEntries.CountAsync(x => x.Action == "ReceiveInventory"));
        Assert.Equal(1, await db.AuditEntries.CountAsync(x => x.Action == "ReconcileInventoryCount"));

        var recentMovements = await service.GetRecentMovementsAsync();
        var recentCounts = await service.GetRecentCountsAsync();
        Assert.Equal(2, recentMovements.Count);
        Assert.Single(recentCounts);
        Assert.Equal("Cooking oil", recentCounts[0].InventoryItemName);
    }

    [Fact]
    public async Task Zero_variance_count_is_preserved_without_creating_a_false_movement()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var scenario = await SeedAsync(database);
        var service = new InventoryService(database.CreateFactory(), new FixedClock());
        await service.ReceiveAsync(
            scenario.ItemId, 5m, "DR-ZERO", null,
            scenario.AdminUsername, "receipt-zero");

        await service.ReconcileCountAsync(
            scenario.ItemId, 5m, "Witnessed opening count",
            scenario.AdminUsername, "count-zero");

        await using var db = database.CreateContext();
        Assert.Single(await db.StockMovements.ToListAsync());
        var count = await db.InventoryCountRecords.SingleAsync();
        Assert.Equal(0m, count.Variance);
        Assert.Equal(5m, await db.StockMovements.SumAsync(x => x.QuantityDelta));
    }

    [Fact]
    public async Task Concurrent_duplicate_delivery_submissions_post_one_receipt()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var scenario = await SeedAsync(database);
        var attempts = Enumerable.Range(0, 8)
            .Select(_ => new InventoryService(database.CreateFactory(), new FixedClock())
                .ReceiveAsync(
                    scenario.ItemId,
                    3m,
                    "DR-CONCURRENT",
                    "One physical delivery",
                    scenario.AdminUsername,
                    "receipt-concurrent"))
            .ToList();

        await Task.WhenAll(attempts);

        await using var db = database.CreateContext();
        var receipt = await db.StockMovements.SingleAsync();
        Assert.Equal(3m, receipt.QuantityDelta);
        Assert.Equal(1, await db.AuditEntries.CountAsync(x => x.Action == "ReceiveInventory"));
    }

    [Fact]
    public async Task Receiving_and_counting_require_admin_and_reject_hostile_ranges()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var scenario = await SeedAsync(database);
        var service = new InventoryService(database.CreateFactory(), new FixedClock());

        await Assert.ThrowsAsync<DomainException>(() =>
            service.ReceiveAsync(
                scenario.ItemId, 1m, "DR-UNAUTHORIZED", null,
                scenario.WaiterUsername, "receipt-unauthorized"));
        await Assert.ThrowsAsync<DomainException>(() =>
            service.ReconcileCountAsync(
                scenario.ItemId, 1m, "Unauthorized count",
                scenario.WaiterUsername, "count-unauthorized"));
        await Assert.ThrowsAsync<DomainException>(() =>
            service.ReceiveAsync(
                scenario.ItemId, 0m, "DR-ZERO", null,
                scenario.AdminUsername, "receipt-invalid"));
        await Assert.ThrowsAsync<DomainException>(() =>
            service.ReconcileCountAsync(
                scenario.ItemId, -0.001m, "Impossible negative count",
                scenario.AdminUsername, "count-invalid"));
        await Assert.ThrowsAsync<DomainException>(() =>
            service.ReceiveAsync(
                scenario.ItemId, 1m, new string('r', 121), null,
                scenario.AdminUsername, "receipt-long-reference"));
        await Assert.ThrowsAsync<DomainException>(() =>
            service.ReceiveAsync(
                scenario.ItemId, 1m, "DR-LONG-NOTE", new string('n', 351),
                scenario.AdminUsername, "receipt-long-note"));
        await Assert.ThrowsAsync<DomainException>(() =>
            service.ReconcileCountAsync(
                scenario.ItemId, 100_000_000_000m, "Impossible huge count",
                scenario.AdminUsername, "count-huge"));
        await Assert.ThrowsAsync<DomainException>(() =>
            service.ReconcileCountAsync(
                scenario.ItemId, 1m, new string('c', 501),
                scenario.AdminUsername, "count-long-reason"));
        await Assert.ThrowsAsync<DomainException>(() => service.GetRecentMovementsAsync(201));
        await Assert.ThrowsAsync<DomainException>(() => service.GetRecentCountsAsync(0));

        await using var db = database.CreateContext();
        Assert.Empty(await db.StockMovements.ToListAsync());
        Assert.Empty(await db.InventoryCountRecords.ToListAsync());
    }

    private static async Task<Scenario> SeedAsync(MariaDbTestDatabase database)
    {
        await using var db = database.CreateContext();
        var adminRole = new IdentityRole(RomsRoles.Admin)
            { NormalizedName = RomsRoles.Admin.ToUpperInvariant() };
        var admin = new ApplicationUser
            { UserName = "inventory-admin", NormalizedUserName = "INVENTORY-ADMIN" };
        var waiter = new ApplicationUser
            { UserName = "inventory-waiter", NormalizedUserName = "INVENTORY-WAITER" };
        var item = new InventoryItem { Name = "Cooking oil", Unit = "ml" };
        db.Roles.Add(adminRole);
        db.Users.AddRange(admin, waiter);
        db.UserRoles.Add(new IdentityUserRole<string>
        {
            UserId = admin.Id,
            RoleId = adminRole.Id
        });
        db.InventoryItems.Add(item);
        await db.SaveChangesAsync();
        return new(item.Id, admin.UserName!, waiter.UserName!);
    }

    private sealed record Scenario(Guid ItemId, string AdminUsername, string WaiterUsername);

    private sealed class FixedClock : IClock
    {
        public DateTime UtcNow => new(2026, 7, 30, 22, 0, 0, DateTimeKind.Utc);
    }
}
