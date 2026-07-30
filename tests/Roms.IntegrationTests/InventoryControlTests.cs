using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Roms.Application;
using Roms.Domain;
using Roms.Infrastructure.Identity;
using Roms.Infrastructure.Persistence;
using Roms.Infrastructure.Services;

namespace Roms.IntegrationTests;

public sealed class InventoryControlTests : IAsyncLifetime
{
    private DbContextOptions<RomsDbContext> options = default!;
    private readonly FixedClock clock = new();
    private Guid itemId;

    public async Task InitializeAsync()
    {
        options = new DbContextOptionsBuilder<RomsDbContext>()
            .UseInMemoryDatabase($"roms-inventory-controls-{Guid.NewGuid()}")
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        await using var db = new RomsDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var adminRole = new IdentityRole(RomsRoles.Admin);
        var kitchenRole = new IdentityRole(RomsRoles.Kitchen);
        var admin = new ApplicationUser { UserName = "admin", NormalizedUserName = "ADMIN" };
        var kitchen = new ApplicationUser { UserName = "kitchen", NormalizedUserName = "KITCHEN" };
        var waiter = new ApplicationUser { UserName = "waiter", NormalizedUserName = "WAITER" };
        db.Roles.AddRange(adminRole, kitchenRole);
        db.Users.AddRange(admin, kitchen, waiter);
        db.UserRoles.AddRange(
            new IdentityUserRole<string> { UserId = admin.Id, RoleId = adminRole.Id },
            new IdentityUserRole<string> { UserId = kitchen.Id, RoleId = kitchenRole.Id });
        var item = new InventoryItem { Name = "Chicken", Unit = "kg" };
        itemId = item.Id;
        item.Movements.Add(new StockMovement
        {
            InventoryItemId = item.Id,
            Type = StockMovementType.Receipt,
            QuantityDelta = 5m,
            Reason = "Opening stock",
            ActorId = "admin",
            IdempotencyKey = "opening-chicken",
            OccurredUtc = clock.UtcNow
        });
        db.InventoryItems.Add(item);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Kitchen_report_is_pending_until_admin_approval_posts_loss()
    {
        var service = CreateService();
        var requestId = await service.ReportLossAsync(
            itemId, InventoryLossType.Waste, 1.25m, "Dropped during prep", "kitchen", "loss-report-1");

        await using (var pendingDb = new RomsDbContext(options))
        {
            Assert.Equal(5m, await pendingDb.StockMovements.SumAsync(x => x.QuantityDelta));
            Assert.Equal(InventoryLossStatus.Pending,
                (await pendingDb.InventoryLossRequests.SingleAsync(x => x.Id == requestId)).Status);
        }

        await Assert.ThrowsAsync<DomainException>(
            () => service.ReviewLossAsync(requestId, true, "Looks valid", "kitchen"));
        await service.ReviewLossAsync(requestId, true, "Verified against shift log", "admin");

        await using var approvedDb = new RomsDbContext(options);
        var movement = await approvedDb.StockMovements.SingleAsync(x => x.IdempotencyKey == $"loss:{requestId}:approved");
        Assert.Equal(StockMovementType.Waste, movement.Type);
        Assert.Equal(-1.25m, movement.QuantityDelta);
        Assert.Equal(3.75m, await approvedDb.StockMovements.SumAsync(x => x.QuantityDelta));
    }

    [Fact]
    public async Task Rejected_spoilage_does_not_change_stock_and_requires_a_reason()
    {
        var service = CreateService();
        var requestId = await service.ReportLossAsync(
            itemId, InventoryLossType.Spoilage, 2m, "Suspected freezer issue", "kitchen", "loss-report-2");

        await Assert.ThrowsAsync<DomainException>(
            () => service.ReviewLossAsync(requestId, false, "", "admin"));
        await service.ReviewLossAsync(requestId, false, "Temperature log was within range", "admin");

        await using var db = new RomsDbContext(options);
        Assert.Equal(5m, await db.StockMovements.SumAsync(x => x.QuantityDelta));
        Assert.Equal(InventoryLossStatus.Rejected,
            (await db.InventoryLossRequests.SingleAsync(x => x.Id == requestId)).Status);
    }

    [Fact]
    public async Task Negative_adjustment_requires_admin_reason_and_emits_discrepancy_alert()
    {
        var service = CreateService();
        await Assert.ThrowsAsync<DomainException>(() =>
            service.AdjustAsync(itemId, -6m, "Count correction", "admin", "adjust-1"));
        await Assert.ThrowsAsync<DomainException>(() =>
            service.AdjustAsync(itemId, -6m, "Count correction", "admin", "adjust-2",
                allowNegativeStock: true));

        await service.AdjustAsync(
            itemId,
            -6m,
            "Count correction",
            "admin",
            "adjust-3",
            allowNegativeStock: true,
            inventoryOverrideReason: "Signed physical count sheet shows shortage");

        await using var db = new RomsDbContext(options);
        Assert.Equal(-1m, await db.StockMovements.SumAsync(x => x.QuantityDelta));
        Assert.True(await db.AuditEntries.AnyAsync(x => x.Action == "INVENTORY_DISCREPANCY_ALERT"));
    }

    [Fact]
    public async Task Waiter_cannot_report_loss_or_manage_inventory()
    {
        var service = CreateService();
        await Assert.ThrowsAsync<DomainException>(() =>
            service.ReportLossAsync(itemId, InventoryLossType.Waste, 1m, "Dropped", "waiter", "loss-report-3"));
        await Assert.ThrowsAsync<DomainException>(() =>
            service.AdjustAsync(itemId, 1m, "Unauthorized receipt", "waiter", "adjust-4"));
    }

    public Task DisposeAsync() => Task.CompletedTask;
    private InventoryService CreateService() => new(new TestFactory(options), clock);

    private sealed class TestFactory(DbContextOptions<RomsDbContext> options) : IDbContextFactory<RomsDbContext>
    {
        public RomsDbContext CreateDbContext() => new(options);
        public Task<RomsDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new RomsDbContext(options));
    }

    private sealed class FixedClock : IClock
    {
        public DateTime UtcNow => new(2026, 7, 30, 10, 0, 0, DateTimeKind.Utc);
    }
}
