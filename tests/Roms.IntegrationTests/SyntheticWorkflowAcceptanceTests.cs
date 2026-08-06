using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Roms.Application;
using Roms.Domain;
using Roms.Infrastructure.Identity;
using Roms.Infrastructure.Persistence;
using Roms.Infrastructure.Services;

namespace Roms.IntegrationTests;

/// <summary>
/// Repeatable four-role acceptance simulation. It deliberately uses a fixed
/// clock and InMemory storage so timer and authorization failures are fast and
/// deterministic; MariaDB/browser acceptance remains a separate gate.
/// </summary>
public sealed class SyntheticWorkflowAcceptanceTests : IAsyncLifetime
{
    private DbContextOptions<RomsDbContext> options = default!;
    private readonly SimulationClock clock = new();

    public async Task InitializeAsync()
    {
        options = new DbContextOptionsBuilder<RomsDbContext>()
            .UseInMemoryDatabase($"roms-synthetic-{Guid.NewGuid()}")
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options;
        await using var db = new RomsDbContext(options);
        var category = new MenuCategory { Name = "Mains" };
        category.Items.Add(new MenuItem { Name = "Burger", Price = 185m, PreparationMinutes = 5 });
        category.Items.Add(new MenuItem { Name = "Chicken", Price = 220m, PreparationMinutes = 10 });
        db.MenuCategories.Add(category);
        db.RestaurantTables.Add(new RestaurantTable { Number = "SIM-1" });

        var roles = new[] { RomsRoles.Waiter, RomsRoles.Kitchen, RomsRoles.Manager, RomsRoles.Admin }
            .Select(x => new IdentityRole(x) { NormalizedName = x.ToUpperInvariant() }).ToArray();
        var users = new[] { "waiter", "kitchen", "manager", "admin" }
            .Select(x => new ApplicationUser { UserName = x, NormalizedUserName = x.ToUpperInvariant(), DisplayName = x }).ToArray();
        db.Roles.AddRange(roles);
        db.Users.AddRange(users);
        db.UserRoles.AddRange(users.Select((u, i) => new IdentityUserRole<string> { UserId = u.Id, RoleId = roles[i].Id }));
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Four_roles_complete_order_with_timers_returns_and_boundaries()
    {
        var workflow = new WorkflowService(new TestFactory(options), clock);
        await workflow.UpdateSettingsAsync(2, 1, "manager");
        var settings = await workflow.GetSettingsAsync();
        Assert.Equal(2, settings.OrderEntryMinutes);
        Assert.Equal(1, settings.KitchenAcceptanceMinutes);

        var orders = CreateOrderService();
        await using var db = new RomsDbContext(options);
        var table = await db.RestaurantTables.SingleAsync();
        var menu = await db.MenuItems.OrderBy(x => x.Name).ToListAsync();
        var orderId = await orders.GetOrCreateDraftAsync(table.Id, "waiter");
        var draft = await orders.GetOrderAsync(orderId, "waiter");
        Assert.Equal(clock.UtcNow.AddMinutes(2), draft!.OrderEntryDueUtc);

        await orders.AddItemAsync(orderId, menu.Single(x => x.Name == "Burger").Id, 2, "No onions", "waiter");
        await orders.AddItemAsync(orderId, menu.Single(x => x.Name == "Chicken").Id, 1, null, "waiter");
        await orders.SubmitAsync(orderId, "synthetic-submit-1", "waiter");
        var submitted = await orders.GetOrderAsync(orderId, "waiter");
        Assert.Equal(clock.UtcNow.AddMinutes(1), submitted!.KitchenAcceptanceDueUtc);

        await Assert.ThrowsAsync<DomainException>(() => orders.TransitionAsync(orderId, OrderStatus.Preparing, "manager"));
        await orders.RequestTimerExtensionAsync(orderId, WorkflowTimerKind.KitchenAcceptance, 3, "Peak queue", "kitchen");
        Assert.Equal(1, await db.OrderTimerExtensions.CountAsync());

        await orders.TransitionAsync(orderId, OrderStatus.ReturnedToWaiter, "kitchen", "Missing side selection");
        await Assert.ThrowsAsync<DomainException>(() => orders.SubmitAsync(orderId, "synthetic-submit-2", "waiter"));
        await orders.SubmitAsync(orderId, "synthetic-submit-2", "waiter", "Side selection corrected");
        await orders.TransitionAsync(orderId, OrderStatus.Preparing, "kitchen");

        var preparing = await orders.GetOrderAsync(orderId, "waiter");
        Assert.Equal(20, preparing!.PreparationTargetMinutes);
        Assert.Equal(clock.UtcNow.AddMinutes(20), preparing.PreparationTargetDueUtc);
        await orders.TransitionAsync(orderId, OrderStatus.Ready, "kitchen");
        await orders.TransitionAsync(orderId, OrderStatus.Completed, "waiter");
        await Assert.ThrowsAsync<DomainException>(() => orders.ConfirmPaymentAsync(orderId, "manager"));
        await orders.ConfirmPaymentAsync(orderId, "admin");
        Assert.Empty(await workflow.GetLiveOrdersAsync());
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private OrderService CreateOrderService() => new(
        new TestFactory(options), clock, new NoOpPublisher(), NullLogger<OrderService>.Instance);

    private sealed class TestFactory(DbContextOptions<RomsDbContext> options) : IDbContextFactory<RomsDbContext>
    {
        public RomsDbContext CreateDbContext() => new(options);
        public Task<RomsDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(new RomsDbContext(options));
    }

    private sealed class SimulationClock : IClock
    {
        public DateTime UtcNow { get; } = new(2026, 8, 6, 12, 0, 0, DateTimeKind.Utc);
    }

    private sealed class NoOpPublisher : IOrderEventPublisher
    {
        public Task PublishAsync(OrderEvent message, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
