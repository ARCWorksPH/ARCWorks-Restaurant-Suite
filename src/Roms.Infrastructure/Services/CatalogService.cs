using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Roms.Application;
using Roms.Domain;
using Roms.Infrastructure.Persistence;

namespace Roms.Infrastructure.Services;

public sealed class CatalogService(IDbContextFactory<RomsDbContext> factory, IClock clock) : ICatalogService
{
    public async Task<IReadOnlyList<RestaurantTable>> GetTablesAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.RestaurantTables.AsNoTracking().OrderBy(x => x.SortOrder).ThenBy(x => x.Number).ToListAsync(ct);
    }

    public async Task SaveTableAsync(RestaurantTable table, string actorId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(table.Number)) throw new DomainException("Table number is required.");
        await using var db = await factory.CreateDbContextAsync(ct);
        var current = await db.RestaurantTables.SingleOrDefaultAsync(x => x.Id == table.Id, ct);
        if (current is null) db.RestaurantTables.Add(table);
        else { current.Number = table.Number.Trim(); current.SortOrder = table.SortOrder; current.IsActive = table.IsActive; }
        db.AuditEntries.Add(Audit(actorId, "SaveTable", table.Id, table));
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<MenuCategory>> GetCategoriesAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.MenuCategories.AsNoTracking().Include(x => x.Items).OrderBy(x => x.SortOrder).ToListAsync(ct);
    }

    public async Task SaveCategoryAsync(MenuCategory category, string actorId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(category.Name)) throw new DomainException("Category name is required.");
        await using var db = await factory.CreateDbContextAsync(ct);
        var current = await db.MenuCategories.SingleOrDefaultAsync(x => x.Id == category.Id, ct);
        if (current is null) db.MenuCategories.Add(category);
        else { current.Name = category.Name.Trim(); current.SortOrder = category.SortOrder; current.IsActive = category.IsActive; }
        db.AuditEntries.Add(Audit(actorId, "SaveCategory", category.Id, category));
        await db.SaveChangesAsync(ct);
    }

    public async Task SaveMenuItemAsync(MenuItem item, string actorId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(item.Name) || item.Price < 0) throw new DomainException("A name and non-negative price are required.");
        await using var db = await factory.CreateDbContextAsync(ct);
        var current = await db.MenuItems.SingleOrDefaultAsync(x => x.Id == item.Id, ct);
        if (current is null) db.MenuItems.Add(item);
        else { current.Name = item.Name.Trim(); current.Description = item.Description.Trim(); current.Price = item.Price;
            current.CategoryId = item.CategoryId; current.IsActive = item.IsActive; current.IsAvailable = item.IsAvailable; }
        db.AuditEntries.Add(Audit(actorId, "SaveMenuItem", item.Id, item));
        await db.SaveChangesAsync(ct);
    }

    private AuditEntry Audit(string actor, string action, Guid id, object values) => new()
        { ActorId = actor, Action = action, EntityType = "Catalog", EntityId = id.ToString(), NewValuesJson = JsonSerializer.Serialize(values), OccurredUtc = clock.UtcNow };
}
