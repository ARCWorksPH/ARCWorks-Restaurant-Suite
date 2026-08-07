using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Roms.Application;
using Roms.Domain;
using Roms.Infrastructure.Identity;
using Roms.Infrastructure.Persistence;
using Roms.Infrastructure.Services;
using Xunit;

namespace Roms.IntegrationTests;

public sealed class ManagerAuthorizationTests : IAsyncLifetime
{
    private DbContextOptions<RomsDbContext> options = default!;
    private readonly TestClock clock = new() { UtcNow = new(2026, 8, 7, 12, 0, 0, DateTimeKind.Utc) };

    public async Task InitializeAsync()
    {
        options = new DbContextOptionsBuilder<RomsDbContext>()
            .UseInMemoryDatabase($"roms-mgr-auth-{Guid.NewGuid()}")
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options;

        await using var db = new RomsDbContext(options);
        await db.Database.EnsureCreatedAsync();

        // Seed roles
        var adminRole = new IdentityRole(RomsRoles.Admin) { NormalizedName = RomsRoles.Admin.ToUpperInvariant() };
        var managerRole = new IdentityRole(RomsRoles.Manager) { NormalizedName = RomsRoles.Manager.ToUpperInvariant() };
        var kitchenRole = new IdentityRole(RomsRoles.Kitchen) { NormalizedName = RomsRoles.Kitchen.ToUpperInvariant() };
        var waiterRole = new IdentityRole(RomsRoles.Waiter) { NormalizedName = RomsRoles.Waiter.ToUpperInvariant() };
        db.Roles.AddRange(adminRole, managerRole, kitchenRole, waiterRole);

        // Seed users
        var admin = new ApplicationUser { Id = "admin-id", UserName = "admin", NormalizedUserName = "ADMIN", DisplayName = "Admin", IsActive = true };
        var manager = new ApplicationUser { Id = "manager-id", UserName = "manager", NormalizedUserName = "MANAGER", DisplayName = "Manager", IsActive = true };
        var kitchen = new ApplicationUser { Id = "kitchen-id", UserName = "kitchen", NormalizedUserName = "KITCHEN", DisplayName = "Kitchen", IsActive = true };
        var waiter = new ApplicationUser { Id = "waiter-id", UserName = "waiter", NormalizedUserName = "WAITER", DisplayName = "Waiter", IsActive = true };
        db.Users.AddRange(admin, manager, kitchen, waiter);

        // Assign roles
        db.UserRoles.Add(new IdentityUserRole<string> { UserId = admin.Id, RoleId = adminRole.Id });
        db.UserRoles.Add(new IdentityUserRole<string> { UserId = manager.Id, RoleId = managerRole.Id });
        db.UserRoles.Add(new IdentityUserRole<string> { UserId = kitchen.Id, RoleId = kitchenRole.Id });
        db.UserRoles.Add(new IdentityUserRole<string> { UserId = waiter.Id, RoleId = waiterRole.Id });

        // Seed a Category and Menu Item
        var category = new MenuCategory { Id = Guid.NewGuid(), Name = "Drinks", IsActive = true };
        var menuItem = new MenuItem { Id = Guid.NewGuid(), CategoryId = category.Id, Name = "Coffee", Price = 100m, PreparationMinutes = 5, IsAvailable = true, IsActive = true };
        db.MenuCategories.Add(category);
        db.MenuItems.Add(menuItem);

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetLiveOrdersAsync_only_allows_Manager_or_Admin()
    {
        var service = new WorkflowService(new TestFactory(options), clock);

        // Allowed
        await service.GetLiveOrdersAsync("admin");
        await service.GetLiveOrdersAsync("manager");

        // Denied
        await Assert.ThrowsAsync<DomainException>(() => service.GetLiveOrdersAsync("kitchen"));
        await Assert.ThrowsAsync<DomainException>(() => service.GetLiveOrdersAsync("waiter"));
    }

    [Fact]
    public async Task GetAdminViewAsync_only_allows_Admin()
    {
        var service = new AttendanceService(new TestFactory(options), clock);

        // Allowed
        await service.GetAdminViewAsync("admin", clock.UtcNow, clock.UtcNow.AddDays(1));

        // Denied
        await Assert.ThrowsAsync<DomainException>(() => service.GetAdminViewAsync("manager", clock.UtcNow, clock.UtcNow.AddDays(1)));
        await Assert.ThrowsAsync<DomainException>(() => service.GetAdminViewAsync("kitchen", clock.UtcNow, clock.UtcNow.AddDays(1)));
    }

    [Fact]
    public async Task GetManagerViewAsync_returns_current_presence_only_for_Manager_or_Admin()
    {
        var service = new AttendanceService(new TestFactory(options), clock);

        // Allowed
        var managerView = await service.GetManagerViewAsync("manager");
        var adminView = await service.GetManagerViewAsync("admin");
        Assert.NotNull(managerView);
        Assert.NotNull(adminView);

        // Denied - Unauthorized
        await Assert.ThrowsAsync<DomainException>(() => service.GetManagerViewAsync("kitchen"));
    }

    [Fact]
    public async Task SaveMenuItemAsync_allows_Kitchen_or_Manager_only_to_toggle_availability()
    {
        var service = new CatalogService(new TestFactory(options), clock);
        await using var db = new RomsDbContext(options);
        var original = await db.MenuItems.FirstAsync();

        // Allowed - Manager toggles IsAvailable
        var updated = new MenuItem
        {
            Id = original.Id,
            CategoryId = original.CategoryId,
            Name = original.Name,
            Description = original.Description,
            Price = original.Price,
            PreparationMinutes = original.PreparationMinutes,
            IsActive = original.IsActive,
            IsAvailable = !original.IsAvailable
        };
        await service.SaveMenuItemAsync(updated, "manager");

        // Manager may restore an item with code 68.
        updated.IsAvailable = true;
        await service.SaveMenuItemAsync(updated, "manager");

        // Kitchen may apply 86, but cannot restore 68.
        updated.IsAvailable = false;
        await service.SaveMenuItemAsync(updated, "kitchen");
        updated.IsAvailable = true;
        await Assert.ThrowsAsync<DomainException>(() => service.SaveMenuItemAsync(updated, "kitchen"));

        // Denied - Manager tries to alter price
        var maliciousPrice = new MenuItem
        {
            Id = original.Id,
            CategoryId = original.CategoryId,
            Name = original.Name,
            Description = original.Description,
            Price = original.Price + 50m,
            PreparationMinutes = original.PreparationMinutes,
            IsActive = original.IsActive,
            IsAvailable = original.IsAvailable
        };
        await Assert.ThrowsAsync<DomainException>(() => service.SaveMenuItemAsync(maliciousPrice, "manager"));

        // Denied - Kitchen tries to change prep minutes
        var maliciousPrep = new MenuItem
        {
            Id = original.Id,
            CategoryId = original.CategoryId,
            Name = original.Name,
            Description = original.Description,
            Price = original.Price,
            PreparationMinutes = original.PreparationMinutes + 5,
            IsActive = original.IsActive,
            IsAvailable = original.IsAvailable
        };
        await Assert.ThrowsAsync<DomainException>(() => service.SaveMenuItemAsync(maliciousPrep, "kitchen"));
    }

    [Fact]
    public async Task Admin_mutations_throw_for_manager_or_kitchen()
    {
        var attendance = new AttendanceService(new TestFactory(options), clock);
        var catalog = new CatalogService(new TestFactory(options), clock);

        await Assert.ThrowsAsync<DomainException>(() => attendance.SaveScheduleAsync(null, "waiter-id", clock.UtcNow, clock.UtcNow.AddHours(8), "shift", "manager"));
        await Assert.ThrowsAsync<DomainException>(() => catalog.SaveTableAsync(new RestaurantTable { Number = "T10" }, "manager"));
        await Assert.ThrowsAsync<DomainException>(() => catalog.SaveCategoryAsync(new MenuCategory { Name = "Apps" }, "kitchen"));
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private sealed class TestFactory(DbContextOptions<RomsDbContext> options) : IDbContextFactory<RomsDbContext>
    {
        public RomsDbContext CreateDbContext() => new(options);
        public Task<RomsDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(new RomsDbContext(options));
    }

    private sealed class TestClock : IClock
    {
        public DateTime UtcNow { get; set; }
    }
}
