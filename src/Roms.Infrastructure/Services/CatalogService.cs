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
        if (table.Number.Trim().Length > 20) throw new DomainException("Table number cannot exceed 20 characters.");
        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureAdminAsync(db, actorId, ct);
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
        if (category.Name.Trim().Length > 80) throw new DomainException("Category name cannot exceed 80 characters.");
        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureAdminAsync(db, actorId, ct);
        var current = await db.MenuCategories.SingleOrDefaultAsync(x => x.Id == category.Id, ct);
        if (current is null) db.MenuCategories.Add(category);
        else { current.Name = category.Name.Trim(); current.SortOrder = category.SortOrder; current.IsActive = category.IsActive; }
        db.AuditEntries.Add(Audit(actorId, "SaveCategory", category.Id, category));
        await db.SaveChangesAsync(ct);
    }

    public async Task SaveMenuItemAsync(MenuItem item, string actorId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(item.Name) || item.Price < 0) throw new DomainException("A name and non-negative price are required.");
        if (item.Name.Trim().Length > 120) throw new DomainException("Menu item name cannot exceed 120 characters.");
        if (item.PreparationMinutes < 1 || item.PreparationMinutes > 1440) throw new DomainException("Preparation time must be between 1 and 1440 minutes.");
        if (item.Description.Trim().Length > 500) throw new DomainException("Menu item description cannot exceed 500 characters.");
        if (item.Price > 9_999_999_999.99m) throw new DomainException("Menu item price is too large.");
        await using var db = await factory.CreateDbContextAsync(ct);

        var roles = await (from user in db.Users
                            join userRole in db.UserRoles on user.Id equals userRole.UserId
                            join role in db.Roles on userRole.RoleId equals role.Id
                            where user.UserName == actorId
                            select role.Name).ToListAsync(ct);

        bool isAdmin = roles.Contains(RomsRoles.Admin);
        bool isManager = roles.Contains(RomsRoles.Manager);
        bool isKitchen = roles.Contains(RomsRoles.Kitchen);
        bool isKitchenOrManager = isKitchen || isManager;

        if (!isAdmin && !isKitchenOrManager)
        {
            throw new DomainException("Unauthorized to modify catalog.");
        }

        var current = await db.MenuItems.SingleOrDefaultAsync(x => x.Id == item.Id, ct);
        if (current is null)
        {
            if (!isAdmin) throw new DomainException("Only an administrator can add new menu items.");
            db.MenuItems.Add(item);
        }
        else
        {
            if (!isAdmin)
            {
                if (isKitchen && !isManager && item.IsAvailable)
                {
                    throw new DomainException("Kitchen staff can only mark menu items unavailable (86). Manager or Admin approval is required to restore (68).");
                }
                if (current.Name.Trim() != item.Name.Trim() ||
                    current.Description.Trim() != item.Description.Trim() ||
                    current.Price != item.Price ||
                    current.PreparationMinutes != item.PreparationMinutes ||
                    current.CategoryId != item.CategoryId ||
                    current.IsActive != item.IsActive)
                {
                    throw new DomainException("Kitchen and Manager roles can only toggle item availability.");
                }
            }
            current.Name = item.Name.Trim();
            current.Description = item.Description.Trim();
            current.Price = item.Price;
            current.PreparationMinutes = item.PreparationMinutes;
            current.CategoryId = item.CategoryId;
            current.IsActive = item.IsActive;
            current.IsAvailable = item.IsAvailable;
        }
        db.AuditEntries.Add(Audit(actorId, "SaveMenuItem", item.Id, item));
        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureAdminAsync(RomsDbContext db, string actorId, CancellationToken ct)
    {
        var allowed = await (from user in db.Users
                             join userRole in db.UserRoles on user.Id equals userRole.UserId
                             join role in db.Roles on userRole.RoleId equals role.Id
                             where user.UserName == actorId && role.Name == RomsRoles.Admin
                             select user.Id).AnyAsync(ct);
        if (!allowed) throw new DomainException("Only an administrator can perform this action.");
    }

    private AuditEntry Audit(string actor, string action, Guid id, object values) => new()
        { ActorId = actor, Action = action, EntityType = "Catalog", EntityId = id.ToString(), NewValuesJson = JsonSerializer.Serialize(values), OccurredUtc = clock.UtcNow };
}
