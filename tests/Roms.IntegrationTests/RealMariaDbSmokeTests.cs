using Microsoft.EntityFrameworkCore;
using Roms.Domain;

namespace Roms.IntegrationTests;

[Collection(MariaDbCollection.Name)]
public sealed class RealMariaDbSmokeTests(MariaDbFixture fixture)
{
    [Fact]
    public async Task Migrations_and_inventory_precision_work_against_MariaDb()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var db = database.CreateContext();

        var item = new InventoryItem { Name = "Cooking Oil", Unit = "ml", MinimumStock = 125.125m };
        item.Movements.Add(new StockMovement
        {
            InventoryItemId = item.Id,
            Type = StockMovementType.Receipt,
            QuantityDelta = 1000.555m,
            Reason = "Disposable database smoke test",
            IdempotencyKey = $"smoke:{Guid.NewGuid():N}",
            OccurredUtc = new DateTime(2026, 7, 29, 0, 0, 0, DateTimeKind.Utc)
        });
        db.InventoryItems.Add(item);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();
        var stored = await db.InventoryItems.Include(x => x.Movements).SingleAsync();
        Assert.Equal(125.125m, stored.MinimumStock);
        Assert.Equal(1000.555m, stored.CurrentStock);
        Assert.Equal("ml", stored.Unit);
    }
}
