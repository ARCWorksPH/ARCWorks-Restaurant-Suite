using Microsoft.EntityFrameworkCore;
using Roms.Domain;
using Roms.ProvisionalImport;

namespace Roms.IntegrationTests;

[Collection(MariaDbCollection.Name)]
public sealed class ProvisionalSeedImportTests(MariaDbFixture fixture)
{
    private const string SourceHash =
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    [Fact]
    public void Preview_reports_supported_and_intentionally_unmapped_fields()
    {
        var preview = ProvisionalSeedValidator.Preview(CreateValidSeed(), SourceHash);

        Assert.True(preview.IsValid);
        Assert.Equal(1, preview.InventoryItemCount);
        Assert.Equal(1, preview.MenuCategoryCount);
        Assert.Equal(1, preview.MenuItemCount);
        Assert.Equal(1, preview.RecipeCount);
        Assert.Equal(1, preview.OpeningBalanceCount);
        Assert.Empty(preview.Errors);
        Assert.Contains(preview.Warnings, warning => warning.Contains("unit cost", StringComparison.Ordinal));
        Assert.Contains(preview.Warnings, warning => warning.Contains("provisional", StringComparison.Ordinal));
    }

    [Fact]
    public void Preview_fails_closed_for_broken_recipe_relationships()
    {
        var seed = CreateValidSeed();
        seed.Recipes[0] = new ProvisionalRecipe
        {
            ExternalId = "REC-001",
            MenuItemExternalId = "MISSING",
            MenuItemName = "Sample Dish",
            InventoryItemExternalId = "INV-001",
            InventoryItemName = "Sample Ingredient",
            Quantity = 0,
            Unit = "kg"
        };

        var preview = ProvisionalSeedValidator.Preview(seed, SourceHash);

        Assert.False(preview.IsValid);
        Assert.Contains(preview.Errors, error => error.Contains("missing menu item", StringComparison.Ordinal));
        Assert.Contains(preview.Errors, error => error.Contains("quantity", StringComparison.Ordinal));
        Assert.Contains(preview.Errors, error => error.Contains("unit does not match", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Import_populates_an_empty_real_MariaDb_sandbox_and_marks_balances_unverified()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var seed = CreateValidSeed();
        var preview = ProvisionalSeedValidator.Preview(seed, SourceHash);

        var result = await ProvisionalSeedImporter.ImportIntoEmptySandboxAsync(
            database.CreateFactory(),
            seed,
            preview,
            confirmEmptySandbox: true);

        Assert.Equal(1, result.InventoryItemsCreated);
        Assert.Equal(1, result.MenuCategoriesCreated);
        Assert.Equal(1, result.MenuItemsCreated);
        Assert.Equal(1, result.RecipesCreated);
        Assert.Equal(1, result.OpeningBalancesCreated);
        await using var db = database.CreateContext();
        var movement = await db.StockMovements.SingleAsync();
        Assert.Equal(25m, movement.QuantityDelta);
        Assert.Equal(StockMovementType.Receipt, movement.Type);
        Assert.Contains("UNVERIFIED", movement.Reason, StringComparison.Ordinal);
        Assert.Equal(new DateTime(2026, 7, 28, 23, 0, 0, DateTimeKind.Utc), movement.OccurredUtc);
        Assert.Equal("ImportProvisionalDataset", (await db.AuditEntries.SingleAsync()).Action);
    }

    [Fact]
    public async Task Import_refuses_a_database_with_existing_operational_data()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var db = database.CreateContext();
        db.InventoryItems.Add(new InventoryItem { Name = "Existing", Unit = "piece" });
        await db.SaveChangesAsync();
        var seed = CreateValidSeed();
        var preview = ProvisionalSeedValidator.Preview(seed, SourceHash);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ProvisionalSeedImporter.ImportIntoEmptySandboxAsync(
                database.CreateFactory(),
                seed,
                preview,
                confirmEmptySandbox: true));

        Assert.Contains("empty sandbox", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static ProvisionalSeed CreateValidSeed() => new()
    {
        InventoryItems =
        [
            new ProvisionalInventoryItem
            {
                ExternalId = "INV-001",
                Name = "Sample Ingredient",
                Category = "Produce",
                Unit = "g",
                OpeningQuantity = 25m,
                CountDateTime = "2026-07-29 07:00:00",
                MinimumStock = 5m,
                UnitCostPhp = 0.25m,
                StorageLocation = "Dry Storage"
            }
        ],
        MenuItems =
        [
            new ProvisionalMenuItem
            {
                ExternalId = "MENU-001",
                Name = "Sample Dish",
                Category = "Mains",
                PricePhp = 150m,
                Description = "A provisional test dish.",
                ServingSize = "1 plate",
                IsAvailable = true
            }
        ],
        Recipes =
        [
            new ProvisionalRecipe
            {
                ExternalId = "REC-001",
                MenuItemExternalId = "MENU-001",
                MenuItemName = "Sample Dish",
                InventoryItemExternalId = "INV-001",
                InventoryItemName = "Sample Ingredient",
                Quantity = 10m,
                Unit = "g"
            }
        ]
    };
}
