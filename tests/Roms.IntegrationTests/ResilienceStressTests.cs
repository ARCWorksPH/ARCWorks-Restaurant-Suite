using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using Roms.Application;
using Roms.Domain;
using Roms.Infrastructure.Identity;
using Roms.Infrastructure.Services;
using Xunit.Abstractions;

namespace Roms.IntegrationTests;

[Collection(MariaDbCollection.Name)]
public sealed class ResilienceStressTests(MariaDbFixture fixture, ITestOutputHelper output)
{
    [Fact]
    public async Task Sixty_simultaneous_waiter_kitchen_cashier_flows_finish_without_lost_updates()
    {
        const int orderCount = 60;
        await using var database = await fixture.CreateDatabaseAsync();
        var scenario = await SeedAsync(database, orderCount, openingStock: null);
        var failures = new ConcurrentBag<Exception>();
        var stopwatch = Stopwatch.StartNew();

        await Parallel.ForEachAsync(
            scenario.TableIds,
            new ParallelOptions { MaxDegreeOfParallelism = 12 },
            async (tableId, _) =>
            {
                try
                {
                    var service = CreateOrderService(database, inventoryEnabled: false);
                    var orderId = await service.GetOrCreateDraftAsync(tableId, scenario.WaiterId);
                    await service.AddItemAsync(orderId, scenario.MenuItemId, 1,
                        "<b>allergy text must stay text</b>", scenario.WaiterId);
                    await service.SubmitAsync(orderId, $"stress-submit:{orderId}", scenario.WaiterId);
                    await service.TransitionAsync(orderId, OrderStatus.Preparing, scenario.KitchenId);
                    await service.TransitionAsync(orderId, OrderStatus.Ready, scenario.KitchenId);
                    await service.TransitionAsync(orderId, OrderStatus.Completed, scenario.WaiterId);
                    await service.ConfirmPaymentAsync(orderId, scenario.AdminId);
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
            });
        stopwatch.Stop();

        Assert.Empty(failures);
        await using var db = database.CreateContext();
        Assert.Equal(orderCount, await db.Orders.CountAsync(x =>
            x.Status == OrderStatus.Completed && x.PaymentConfirmedUtc != null));
        Assert.Equal(orderCount * 4, await db.OrderStatusHistory.CountAsync());
        Assert.Equal(orderCount * 7, await db.AuditEntries.CountAsync());
        Assert.Equal(orderCount, await db.IdempotencyRecords.CountAsync());
        Assert.All(await db.OrderItems.Select(x => x.Notes).ToListAsync(),
            note => Assert.Equal("<b>allergy text must stay text</b>", note));

        // This is a bounded regression ceiling, not a capacity promise.
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(90),
            $"The bounded 60-order run took {stopwatch.Elapsed}.");
    }

    [Fact]
    public async Task Twenty_four_competing_tickets_cannot_spend_more_than_twelve_units()
    {
        const int orderCount = 24;
        const int availableUnits = 12;
        await using var database = await fixture.CreateDatabaseAsync();
        var scenario = await SeedAsync(database, orderCount, openingStock: availableUnits);
        var orderIds = new List<Guid>();
        var setup = CreateOrderService(database, inventoryEnabled: true);
        foreach (var tableId in scenario.TableIds)
        {
            var orderId = await setup.GetOrCreateDraftAsync(tableId, scenario.WaiterId);
            await setup.AddItemAsync(orderId, scenario.MenuItemId, 1, null, scenario.WaiterId);
            await setup.SubmitAsync(orderId, $"burst-submit:{orderId}", scenario.WaiterId);
            orderIds.Add(orderId);
        }

        var outcomes = new ConcurrentBag<Exception?>();
        var databaseConflicts = new ConcurrentBag<Exception>();
        var conflictLogger = new ConflictRecordingLogger(databaseConflicts);
        await Parallel.ForEachAsync(
            orderIds,
            new ParallelOptions { MaxDegreeOfParallelism = 8 },
            async (orderId, _) =>
            {
                try
                {
                    await CreateOrderService(database, inventoryEnabled: true, conflictLogger)
                        .TransitionAsync(orderId, OrderStatus.Preparing, scenario.KitchenId);
                    outcomes.Add(null);
                }
                catch (Exception exception) when (
                    exception is DomainException or DbUpdateException)
                {
                    outcomes.Add(exception);
                }
            });

        Assert.Equal(orderCount, outcomes.Count);
        var initialSuccesses = outcomes.Count(x => x is null);
        Assert.InRange(initialSuccesses, 1, availableUnits);
        Assert.All(outcomes.Where(x => x is not null), exception =>
            Assert.IsType<DomainException>(exception));
        var mysqlConflicts = databaseConflicts
            .SelectMany(ExceptionChain)
            .OfType<MySqlException>()
            .Where(x => x.Number is 1205 or 1213)
            .ToList();
        output.WriteLine(
            "Initial surge: {0} committed, {1} rejected; captured MariaDB transaction conflicts: {2}.",
            initialSuccesses,
            orderCount - initialSuccesses,
            mysqlConflicts.Count);
        foreach (var group in mysqlConflicts.GroupBy(x => new { x.Number, x.SqlState, x.Message }))
        {
            output.WriteLine(
                "MariaDB error {0}, SQLSTATE {1}, occurrences {2}: {3}",
                group.Key.Number,
                group.Key.SqlState,
                group.Count(),
                group.Key.Message);
        }
        var innodbDeadlock = await ReadLatestDeadlockAsync(database.ConnectionString);
        if (!string.IsNullOrWhiteSpace(innodbDeadlock))
            output.WriteLine("InnoDB latest-deadlock excerpt:{0}{1}", Environment.NewLine, innodbDeadlock);

        // A deliberately excessive first surge may cause transient database deadlocks.
        // Once contention subsides, retry the still-New tickets as a real client would.
        await using (var retryDb = database.CreateContext())
        {
            var retryIds = await retryDb.Orders
                .Where(x => x.Status == OrderStatus.New)
                .Select(x => x.Id)
                .ToListAsync();
            foreach (var orderId in retryIds)
            {
                try
                {
                    await CreateOrderService(database, inventoryEnabled: true)
                        .TransitionAsync(orderId, OrderStatus.Preparing, scenario.KitchenId);
                }
                catch (DomainException exception) when (
                    exception.Message.StartsWith("Insufficient stock:", StringComparison.Ordinal))
                {
                    // Expected after all twelve available units are committed.
                }
            }
        }

        await using var db = database.CreateContext();
        Assert.Equal(availableUnits, await db.Orders.CountAsync(x => x.Status == OrderStatus.Preparing));
        Assert.Equal(orderCount - availableUnits, await db.Orders.CountAsync(x => x.Status == OrderStatus.New));
        Assert.Equal(0m, await db.StockMovements.SumAsync(x => x.QuantityDelta));
        Assert.Equal(availableUnits, await db.StockMovements.CountAsync(x => x.OrderId != null));
    }

    private static async Task<Scenario> SeedAsync(
        MariaDbTestDatabase database,
        int orderCount,
        decimal? openingStock)
    {
        await using var db = database.CreateContext();
        var category = new MenuCategory { Name = "Stress menu" };
        var menuItem = new MenuItem { Name = "Synthetic meal", Price = 100m };
        category.Items.Add(menuItem);
        if (openingStock is not null)
        {
            var inventoryItem = new InventoryItem { Name = "Synthetic unit", Unit = "piece" };
            inventoryItem.Movements.Add(new StockMovement
            {
                InventoryItemId = inventoryItem.Id,
                Type = StockMovementType.Receipt,
                QuantityDelta = openingStock.Value,
                Reason = "Stress-test opening stock",
                IdempotencyKey = $"stress-opening:{inventoryItem.Id}",
                ActorId = "admin",
                OccurredUtc = FixedClock.Value
            });
            menuItem.RecipeIngredients.Add(new RecipeIngredient
            {
                InventoryItem = inventoryItem,
                Quantity = 1m
            });
        }

        var adminRole = new IdentityRole(RomsRoles.Admin)
            { NormalizedName = RomsRoles.Admin.ToUpperInvariant() };
        var kitchenRole = new IdentityRole(RomsRoles.Kitchen)
            { NormalizedName = RomsRoles.Kitchen.ToUpperInvariant() };
        var waiter = new ApplicationUser
            { UserName = "stress-waiter", NormalizedUserName = "STRESS-WAITER", DisplayName = "Stress Waiter" };
        var kitchen = new ApplicationUser
            { UserName = "stress-kitchen", NormalizedUserName = "STRESS-KITCHEN", DisplayName = "Stress Kitchen" };
        var admin = new ApplicationUser
            { UserName = "stress-admin", NormalizedUserName = "STRESS-ADMIN", DisplayName = "Stress Admin" };
        var tables = Enumerable.Range(1, orderCount)
            .Select(x => new RestaurantTable { Number = $"S{x:00}", SortOrder = x })
            .ToList();

        db.MenuCategories.Add(category);
        db.RestaurantTables.AddRange(tables);
        db.Roles.AddRange(adminRole, kitchenRole);
        db.Users.AddRange(waiter, kitchen, admin);
        db.UserRoles.AddRange(
            new IdentityUserRole<string> { UserId = kitchen.Id, RoleId = kitchenRole.Id },
            new IdentityUserRole<string> { UserId = admin.Id, RoleId = adminRole.Id });
        await db.SaveChangesAsync();
        return new Scenario(
            tables.Select(x => x.Id).ToList(),
            menuItem.Id,
            waiter.UserName!,
            kitchen.UserName!,
            admin.UserName!);
    }

    private static OrderService CreateOrderService(
        MariaDbTestDatabase database,
        bool inventoryEnabled,
        ILogger<OrderService>? logger = null) =>
        new(
            database.CreateFactory(),
            new FixedClock(),
            new NoOpPublisher(),
            Options.Create(new InventoryOptions { Enabled = inventoryEnabled }),
            logger ?? NullLogger<OrderService>.Instance);

    private static IEnumerable<Exception> ExceptionChain(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
            yield return current;
    }

    private static async Task<string?> ReadLatestDeadlockAsync(string connectionString)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SHOW ENGINE INNODB STATUS";
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        var status = reader.GetString(2);
        const string marker = "LATEST DETECTED DEADLOCK";
        var start = status.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0) return null;
        var end = status.IndexOf("\n------------\nTRANSACTIONS", start, StringComparison.Ordinal);
        return (end < 0 ? status[start..] : status[start..end]).Trim();
    }

    private sealed record Scenario(
        IReadOnlyList<Guid> TableIds,
        Guid MenuItemId,
        string WaiterId,
        string KitchenId,
        string AdminId);

    private sealed class FixedClock : IClock
    {
        public static readonly DateTime Value = new(2026, 7, 30, 14, 0, 0, DateTimeKind.Utc);
        public DateTime UtcNow => Value;
    }

    private sealed class NoOpPublisher : IOrderEventPublisher
    {
        public Task PublishAsync(OrderEvent message, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class ConflictRecordingLogger(ConcurrentBag<Exception> exceptions)
        : ILogger<OrderService>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (exception is not null && logLevel >= LogLevel.Warning)
                exceptions.Add(exception);
        }
    }
}
