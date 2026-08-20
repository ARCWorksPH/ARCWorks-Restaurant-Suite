using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Roms.Application;
using Roms.Domain;
using Roms.Infrastructure.Identity;
using Roms.Infrastructure.Persistence;

namespace Roms.Web;

public sealed class SeedOptions
{
    public string AdminUsername { get; set; } = "admin";
    public string AdminPassword { get; set; } = "";
    public string AdminDisplayName { get; set; } = ProductBrand.Name + " Administrator";
    public bool DemoData { get; set; }
}

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, IWebHostEnvironment environment)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RomsDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { RomsRoles.Admin, RomsRoles.Waiter, RomsRoles.Kitchen, RomsRoles.Manager })
            if (!await roleManager.RoleExistsAsync(role)) await roleManager.CreateAsync(new IdentityRole(role));

        var options = scope.ServiceProvider.GetRequiredService<IOptions<SeedOptions>>().Value;
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var restaurantClock = scope.ServiceProvider.GetRequiredService<IRestaurantClock>();

        // Earlier soft-deletion retained the original login name, which prevented a
        // restaurant from reusing a departed employee's username. Repair those rows
        // at startup without deleting their immutable Identity IDs or history.
        var inactiveUsers = await userManager.Users
            .Where(x => !x.IsActive && (x.ArchivedUserName == null || !x.UserName!.StartsWith("__archived__")))
            .ToListAsync();
        foreach (var inactiveUser in inactiveUsers)
        {
            var oldUserName = inactiveUser.ArchivedUserName ?? inactiveUser.UserName ?? "staff";
            inactiveUser.ArchivedUserName = oldUserName;
            inactiveUser.UserName = ApplicationUser.BuildArchivedUserName(inactiveUser.Id, oldUserName);
            inactiveUser.NormalizedUserName = userManager.NormalizeName(inactiveUser.UserName);
            inactiveUser.ActiveSessionId = null;
            inactiveUser.ActiveApplicationInstanceId = null;
            inactiveUser.SessionLastActivityUtc = null;
            var archiveResult = await userManager.UpdateAsync(inactiveUser);
            if (!archiveResult.Succeeded)
                throw new InvalidOperationException(string.Join("; ", archiveResult.Errors.Select(x => x.Description)));

            db.AuditEntries.Add(new AuditEntry
            {
                ActorId = "system",
                Action = "ArchiveInactiveUserName",
                EntityType = nameof(ApplicationUser),
                EntityId = inactiveUser.Id,
                OldValuesJson = System.Text.Json.JsonSerializer.Serialize(new { IsActive = false, UserName = oldUserName }),
                NewValuesJson = System.Text.Json.JsonSerializer.Serialize(new { IsActive = false, ArchivedUserName = oldUserName }),
                OccurredUtc = DateTime.UtcNow
            });
        }
        if (inactiveUsers.Count > 0) await db.SaveChangesAsync();

        var admin = await userManager.FindByNameAsync(options.AdminUsername);
        if (admin is null && string.IsNullOrWhiteSpace(options.AdminPassword))
        {
            if (!environment.IsDevelopment())
                throw new InvalidOperationException(
                    "Seed:AdminPassword is required only for the initial production administrator bootstrap.");
        }
        else if (admin is null)
        {
            admin = new ApplicationUser { UserName = options.AdminUsername, DisplayName = options.AdminDisplayName, EmailConfirmed = true };
            var result = await userManager.CreateAsync(admin, options.AdminPassword);
            if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
        }
        if (admin is not null && !await userManager.IsInRoleAsync(admin, RomsRoles.Admin))
            await userManager.AddToRoleAsync(admin, RomsRoles.Admin);

        if (options.DemoData && !await db.RestaurantTables.AnyAsync())
        {
            db.RestaurantTables.AddRange(Enumerable.Range(1, 12).Select(i => new RestaurantTable { Number = i.ToString(), SortOrder = i }));
            var mains = new MenuCategory { Name = "Mains", SortOrder = 1 };
            var drinks = new MenuCategory { Name = "Drinks", SortOrder = 2 };
            mains.Items.AddRange([
                new MenuItem { Name = "Cheeseburger", Description = "Beef patty, cheese, and house sauce", Price = 185m },
                new MenuItem { Name = "Chicken Rice", Description = "Grilled chicken with steamed rice", Price = 165m }
            ]);
            drinks.Items.AddRange([
                new MenuItem { Name = "Iced Tea", Description = "House-brewed iced tea", Price = 55m },
                new MenuItem { Name = "Bottled Water", Price = 35m }
            ]);
            db.MenuCategories.AddRange(mains, drinks);
            await db.SaveChangesAsync();
        }

        if (options.DemoData && environment.IsDevelopment())
            await DemoStaffFixtures.EnsureAsync(db, userManager, restaurantClock);
    }
}
