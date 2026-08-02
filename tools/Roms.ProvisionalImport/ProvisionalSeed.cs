using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Roms.Domain;
using Roms.Infrastructure.Persistence;

namespace Roms.ProvisionalImport;

public sealed class ProvisionalSeed
{
    [JsonPropertyName("inventory_items")]
    public List<ProvisionalInventoryItem> InventoryItems { get; init; } = [];

    [JsonPropertyName("menu_items")]
    public List<ProvisionalMenuItem> MenuItems { get; init; } = [];

}

public sealed class ProvisionalInventoryItem
{
    [JsonPropertyName("Inventory_Item_ID")]
    public string ExternalId { get; init; } = "";

    [JsonPropertyName("Inventory_Item_Name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("Category")]
    public string Category { get; init; } = "";

    [JsonPropertyName("Canonical_Unit")]
    public string Unit { get; init; } = "";

    [JsonPropertyName("Current_Opening_Quantity")]
    public decimal OpeningQuantity { get; init; }

    [JsonPropertyName("Count_Date_Time")]
    public string CountDateTime { get; init; } = "";

    [JsonPropertyName("Minimum_Stock_Alert_Level")]
    public decimal MinimumStock { get; init; }

    [JsonPropertyName("Unit_Cost_PHP")]
    public decimal UnitCostPhp { get; init; }

    [JsonPropertyName("Storage_Location")]
    public string StorageLocation { get; init; } = "";
}

public sealed class ProvisionalMenuItem
{
    [JsonPropertyName("Menu_Item_ID")]
    public string ExternalId { get; init; } = "";

    [JsonPropertyName("Menu_Item_Name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("Category")]
    public string Category { get; init; } = "";

    [JsonPropertyName("Selling_Price_PHP")]
    public decimal PricePhp { get; init; }

    [JsonPropertyName("Description")]
    public string Description { get; init; } = "";

    [JsonPropertyName("Serving_Size")]
    public string ServingSize { get; init; } = "";

    [JsonPropertyName("Is_Available")]
    public bool IsAvailable { get; init; }
}

public sealed record ProvisionalImportPreview(
    string SourceSha256,
    bool IsValid,
    int InventoryItemCount,
    int MenuCategoryCount,
    int MenuItemCount,
    int OpeningBalanceCount,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings);

public sealed record ProvisionalImportResult(
    string SourceSha256,
    int InventoryItemsCreated,
    int MenuCategoriesCreated,
    int MenuItemsCreated,
    int OpeningBalancesCreated);

public static class ProvisionalSeedLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = false
    };

    public static async Task<(ProvisionalSeed Seed, string Sha256)> LoadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var bytes = await File.ReadAllBytesAsync(path, cancellationToken);
        var seed = JsonSerializer.Deserialize<ProvisionalSeed>(bytes, JsonOptions)
            ?? throw new InvalidDataException("The provisional seed JSON is empty.");
        return (seed, Convert.ToHexString(SHA256.HashData(bytes)));
    }
}

public static class ProvisionalSeedValidator
{
    private static readonly HashSet<string> AllowedUnits = new(StringComparer.Ordinal)
    {
        "piece",
        "g",
        "ml"
    };

    public static ProvisionalImportPreview Preview(ProvisionalSeed seed, string sourceSha256)
    {
        var errors = new List<string>();
        var warnings = new List<string>
        {
            "All source values are provisional and require restaurant confirmation before production use.",
            "Inventory category, unit cost, and storage location are validated but not imported by the Phase 1 schema.",
            "Serving size is validated but not imported by the Phase 1 schema.",
            "Recipe data is intentionally ignored because recipe functionality is outside the approved product scope.",
            "Employee permissions, waste scenarios, contact information, and negative-stock policy are not imported."
        };
        if (sourceSha256.Length != 64 || sourceSha256.Any(character => !Uri.IsHexDigit(character)))
            errors.Add("The source SHA-256 must be a 64-character hexadecimal value.");

        ValidateUnique(seed.InventoryItems, item => item.ExternalId, "inventory item", errors);
        ValidateUnique(seed.MenuItems, item => item.ExternalId, "menu item", errors);

        foreach (var item in seed.InventoryItems)
        {
            Length(item.ExternalId, 50, "Inventory item ID", errors);
            Length(item.Name, 120, $"Inventory item {item.ExternalId} name", errors);
            if (!AllowedUnits.Contains(item.Unit))
                errors.Add($"Inventory item {item.ExternalId} uses unsupported unit '{item.Unit}'.");
            if (!FitsQuantity(item.OpeningQuantity) || item.OpeningQuantity < 0)
                errors.Add($"Inventory item {item.ExternalId} has an invalid opening quantity.");
            if (!FitsQuantity(item.MinimumStock) || item.MinimumStock < 0)
                errors.Add($"Inventory item {item.ExternalId} has an invalid minimum stock.");
            if (item.UnitCostPhp < 0)
                errors.Add($"Inventory item {item.ExternalId} has a negative unit cost.");
            if (!DateTime.TryParseExact(
                    item.CountDateTime,
                    "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out _))
                errors.Add($"Inventory item {item.ExternalId} has an invalid count date/time.");
        }

        foreach (var item in seed.MenuItems)
        {
            Length(item.ExternalId, 50, "Menu item ID", errors);
            Length(item.Name, 120, $"Menu item {item.ExternalId} name", errors);
            Length(item.Description, 500, $"Menu item {item.ExternalId} description", errors);
            Length(item.Category, 80, $"Menu item {item.ExternalId} category", errors);
            if (!FitsMoney(item.PricePhp) || item.PricePhp < 0)
                errors.Add($"Menu item {item.ExternalId} has an invalid price.");
        }

        return new ProvisionalImportPreview(
            sourceSha256,
            errors.Count == 0,
            seed.InventoryItems.Count,
            seed.MenuItems.Select(item => item.Category).Distinct(StringComparer.Ordinal).Count(),
            seed.MenuItems.Count,
            seed.InventoryItems.Count(item => item.OpeningQuantity != 0),
            errors,
            warnings);
    }

    private static void ValidateUnique<T>(
        IEnumerable<T> values,
        Func<T, string> keySelector,
        string label,
        ICollection<string> errors)
    {
        foreach (var key in values.GroupBy(keySelector, StringComparer.Ordinal).Where(group => group.Count() > 1))
            errors.Add($"Duplicate {label} ID '{key.Key}'.");
    }

    private static void Required(string value, string label, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add($"{label} is required.");
    }

    private static void Length(string value, int maximum, string label, ICollection<string> errors)
    {
        Required(value, label, errors);
        if (value.Length > maximum)
            errors.Add($"{label} exceeds {maximum} characters.");
    }

    private static bool FitsQuantity(decimal value) =>
        value <= 99_999_999_999.999m && DecimalScale(value) <= 3;

    private static bool FitsMoney(decimal value) =>
        value <= 9_999_999_999.99m && DecimalScale(value) <= 2;

    private static int DecimalScale(decimal value) =>
        (decimal.GetBits(value)[3] >> 16) & 0x7F;
}

public static class ProvisionalSeedImporter
{
    public static async Task<ProvisionalImportResult> ImportIntoEmptySandboxAsync(
        IDbContextFactory<RomsDbContext> factory,
        ProvisionalSeed seed,
        ProvisionalImportPreview preview,
        bool confirmEmptySandbox,
        CancellationToken cancellationToken = default)
    {
        if (!confirmEmptySandbox)
            throw new InvalidOperationException("The explicit empty-sandbox confirmation is required.");
        if (!preview.IsValid)
            throw new InvalidOperationException("The provisional dataset failed validation.");

        await using var strategyContext = await factory.CreateDbContextAsync(cancellationToken);
        var strategy = strategyContext.Database.CreateExecutionStrategy();
        ProvisionalImportResult? result = null;
        await strategy.ExecuteAsync(async () =>
        {
            await using var db = await factory.CreateDbContextAsync(cancellationToken);
            var hasOperationalData =
                await db.Orders.AnyAsync(cancellationToken) ||
                await db.RestaurantTables.AnyAsync(cancellationToken) ||
                await db.MenuCategories.AnyAsync(cancellationToken) ||
                await db.MenuItems.AnyAsync(cancellationToken) ||
                await db.InventoryItems.AnyAsync(cancellationToken) ||
                await db.StockMovements.AnyAsync(cancellationToken);
            if (hasOperationalData)
                throw new InvalidOperationException("Import is allowed only into an empty sandbox database.");

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var categoryIds = seed.MenuItems
                .Select(item => item.Category.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToDictionary(name => name, name => StableGuid($"category:{name}"), StringComparer.Ordinal);
            var inventoryIds = seed.InventoryItems.ToDictionary(
                item => item.ExternalId,
                item => StableGuid($"inventory:{item.ExternalId}"),
                StringComparer.Ordinal);
            var menuIds = seed.MenuItems.ToDictionary(
                item => item.ExternalId,
                item => StableGuid($"menu:{item.ExternalId}"),
                StringComparer.Ordinal);

            var categories = categoryIds
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select((pair, index) => new MenuCategory
                {
                    Id = pair.Value,
                    Name = pair.Key,
                    SortOrder = index + 1,
                    IsActive = true
                })
                .ToList();
            db.MenuCategories.AddRange(categories);

            db.InventoryItems.AddRange(seed.InventoryItems.Select(item => new InventoryItem
            {
                Id = inventoryIds[item.ExternalId],
                Name = item.Name.Trim(),
                Unit = item.Unit,
                MinimumStock = item.MinimumStock,
                IsActive = true
            }));

            db.MenuItems.AddRange(seed.MenuItems.Select(item => new MenuItem
            {
                Id = menuIds[item.ExternalId],
                CategoryId = categoryIds[item.Category.Trim()],
                Name = item.Name.Trim(),
                Description = item.Description.Trim(),
                Price = item.PricePhp,
                IsActive = true,
                IsAvailable = item.IsAvailable
            }));

            var openingBalances = seed.InventoryItems
                .Where(item => item.OpeningQuantity != 0)
                .Select(item => new StockMovement
                {
                    InventoryItemId = inventoryIds[item.ExternalId],
                    Type = StockMovementType.Receipt,
                    QuantityDelta = item.OpeningQuantity,
                    Reason = "UNVERIFIED provisional opening balance imported into sandbox",
                    IdempotencyKey = $"provisional:{preview.SourceSha256[..16]}:opening:{item.ExternalId}",
                    ActorId = "provisional-import",
                    OccurredUtc = ParseManilaCountTime(item.CountDateTime)
                })
                .ToList();
            db.StockMovements.AddRange(openingBalances);
            db.AuditEntries.Add(new AuditEntry
            {
                ActorId = "provisional-import",
                Action = "ImportProvisionalDataset",
                EntityType = "ProvisionalDataset",
                EntityId = preview.SourceSha256,
                Reason = "Sandbox-only import; all restaurant values remain unverified.",
                NewValuesJson = JsonSerializer.Serialize(new
                {
                    preview.InventoryItemCount,
                    preview.MenuCategoryCount,
                    preview.MenuItemCount,
                    preview.OpeningBalanceCount
                }),
                OccurredUtc = DateTime.UtcNow
            });

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            result = new ProvisionalImportResult(
                preview.SourceSha256,
                seed.InventoryItems.Count,
                categories.Count,
                seed.MenuItems.Count,
                openingBalances.Count);
        });
        return result!;
    }

    private static Guid StableGuid(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"ROMS provisional import\u001f{value}"));
        return new Guid(hash.AsSpan(0, 16));
    }

    private static DateTime ParseManilaCountTime(string value)
    {
        var local = DateTime.SpecifyKind(
            DateTime.ParseExact(
                value,
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None),
            DateTimeKind.Unspecified);
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
        return TimeZoneInfo.ConvertTimeToUtc(local, timeZone);
    }
}
