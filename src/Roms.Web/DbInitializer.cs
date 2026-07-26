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
    public string AdminDisplayName { get; set; } = "ROMS Administrator";
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
        foreach (var role in new[] { RomsRoles.Admin, RomsRoles.Waiter, RomsRoles.Kitchen })
            if (!await roleManager.RoleExistsAsync(role)) await roleManager.CreateAsync(new IdentityRole(role));

        var options = scope.ServiceProvider.GetRequiredService<IOptions<SeedOptions>>().Value;
        if (string.IsNullOrWhiteSpace(options.AdminPassword))
        {
            if (!environment.IsDevelopment())
                throw new InvalidOperationException("Seed:AdminPassword (or ROMS_Seed__AdminPassword) is required in production.");
        }
        else
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var admin = await userManager.FindByNameAsync(options.AdminUsername);
            if (admin is null)
            {
                admin = new ApplicationUser { UserName = options.AdminUsername, DisplayName = options.AdminDisplayName, EmailConfirmed = true };
                var result = await userManager.CreateAsync(admin, options.AdminPassword);
                if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
            }
            if (!await userManager.IsInRoleAsync(admin, RomsRoles.Admin)) await userManager.AddToRoleAsync(admin, RomsRoles.Admin);
        }

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
    }
}
