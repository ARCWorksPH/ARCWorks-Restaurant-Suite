using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Roms.Application;
using Roms.Application.Ai;
using Roms.Application.Commands;
using Roms.Domain;
using Roms.Infrastructure.Identity;
using Roms.Infrastructure.Persistence;
using Roms.Infrastructure.Services;

namespace Roms.IntegrationTests;

public sealed class AiFunctionServiceTests
{
    [Fact]
    public async Task Menu_functions_return_stored_facts_and_hide_prices_from_kitchen()
    {
        var scenario = await CreateScenarioAsync();

        var waiter = await scenario.Service.ExecuteAsync(
            new AiFunctionRequest(AiFunctionName.GetMenuItem, ItemName: "cheeseburger"),
            scenario.WaiterOne);
        var kitchen = await scenario.Service.ExecuteAsync(
            new AiFunctionRequest(AiFunctionName.GetMenuItem, ItemName: "Cheeseburger"),
            scenario.Kitchen);
        var unavailable = await scenario.Service.ExecuteAsync(
            new AiFunctionRequest(AiFunctionName.ListMenu, Available: false),
            scenario.Admin);

        Assert.Equal(AiFunctionStatus.Success, waiter.Status);
        Assert.Equal(185m, Assert.IsType<AiMenuItemFact>(waiter.Data).Price);
        Assert.Null(Assert.IsType<AiMenuItemFact>(kitchen.Data).Price);
        var unavailableItems = Assert.IsAssignableFrom<IReadOnlyList<AiMenuItemFact>>(unavailable.Data);
        Assert.Single(unavailableItems);
        Assert.Equal("Cola", unavailableItems[0].Name);
    }

    [Fact]
    public async Task Inventory_functions_allow_admin_and_kitchen_but_deny_waiter()
    {
        var scenario = await CreateScenarioAsync();

        var kitchen = await scenario.Service.ExecuteAsync(
            new AiFunctionRequest(AiFunctionName.GetInventoryBalance, ItemName: "Cooking oil"),
            scenario.Kitchen);
        var lowStock = await scenario.Service.ExecuteAsync(
            new AiFunctionRequest(AiFunctionName.GetLowStockSummary),
            scenario.Admin);
        var waiter = await scenario.Service.ExecuteAsync(
            new AiFunctionRequest(AiFunctionName.ListInventoryBalances),
            scenario.WaiterOne);

        var oil = Assert.IsType<AiInventoryFact>(kitchen.Data);
        Assert.Equal(8m, oil.CurrentStock);
        Assert.True(oil.IsLowStock);
        var summary = Assert.IsType<AiLowStockSummaryFact>(lowStock.Data);
        Assert.Equal(1, summary.LowStockCount);
        Assert.Equal(AiFunctionStatus.Unauthorized, waiter.Status);
        Assert.Null(waiter.Data);
    }

    [Fact]
    public async Task Order_functions_enforce_owner_kitchen_queue_and_admin_boundaries()
    {
        var scenario = await CreateScenarioAsync();

        var ownPaid = await scenario.Service.ExecuteAsync(
            new AiFunctionRequest(AiFunctionName.GetOrderStatus, OrderId: scenario.PaidOrderId),
            scenario.WaiterOne);
        var otherWaiter = await scenario.Service.ExecuteAsync(
            new AiFunctionRequest(AiFunctionName.GetOrderStatus, OrderId: scenario.PreparingOrderId),
            scenario.WaiterOne);
        var kitchenActive = await scenario.Service.ExecuteAsync(
            new AiFunctionRequest(AiFunctionName.GetOrderStatus, OrderId: scenario.PreparingOrderId),
            scenario.Kitchen);
        var kitchenPaid = await scenario.Service.ExecuteAsync(
            new AiFunctionRequest(AiFunctionName.GetOrderStatus, OrderId: scenario.PaidOrderId),
            scenario.Kitchen);

        Assert.Equal(AiFunctionStatus.Success, ownPaid.Status);
        Assert.Equal(AiFunctionStatus.Unauthorized, otherWaiter.Status);
        var active = Assert.IsType<AiOrderStatusFact>(kitchenActive.Data);
        Assert.Equal(OrderStatus.Preparing, active.Status);
        Assert.Null(active.Total);
        Assert.All(active.Items, item => Assert.Null(item.UnitPrice));
        Assert.Equal(AiFunctionStatus.Unauthorized, kitchenPaid.Status);
    }

    [Fact]
    public async Task Order_lists_use_actual_statuses_and_restrict_history()
    {
        var scenario = await CreateScenarioAsync();

        var kitchen = await scenario.Service.ExecuteAsync(
            new AiFunctionRequest(AiFunctionName.ListOrdersByStatus, OrderStatus: OrderStatus.Preparing),
            scenario.Kitchen);
        var kitchenCompleted = await scenario.Service.ExecuteAsync(
            new AiFunctionRequest(AiFunctionName.ListOrdersByStatus, OrderStatus: OrderStatus.Completed),
            scenario.Kitchen);
        var waiterCancelled = await scenario.Service.ExecuteAsync(
            new AiFunctionRequest(AiFunctionName.ListOrdersByStatus, OrderStatus: OrderStatus.Cancelled),
            scenario.WaiterOne);

        Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<AiOrderStatusFact>>(kitchen.Data));
        Assert.Equal(AiFunctionStatus.Unauthorized, kitchenCompleted.Status);
        Assert.Equal(AiFunctionStatus.Unauthorized, waiterCancelled.Status);
    }

    [Fact]
    public async Task Admin_summaries_use_Manila_business_day_and_paid_completed_value()
    {
        var scenario = await CreateScenarioAsync();

        var daily = await scenario.Service.ExecuteAsync(
            new AiFunctionRequest(AiFunctionName.GetDailyOrderSummary, BusinessDate: new DateOnly(2026, 8, 2)),
            scenario.Admin);
        var operational = await scenario.Service.ExecuteAsync(
            new AiFunctionRequest(AiFunctionName.GetOperationalSummary, BusinessDate: new DateOnly(2026, 8, 2)),
            scenario.Admin);
        var waiter = await scenario.Service.ExecuteAsync(
            new AiFunctionRequest(AiFunctionName.GetDailyOrderSummary, BusinessDate: new DateOnly(2026, 8, 2)),
            scenario.WaiterOne);

        var dailyFact = Assert.IsType<AiDailyOrderSummaryFact>(daily.Data);
        Assert.Equal("Asia/Manila", dailyFact.TimeZone);
        Assert.Equal(1, dailyFact.PaidCompletedOrders);
        Assert.Equal(1, dailyFact.CancelledOrders);
        Assert.Equal(185m, dailyFact.PaidCompletedOrderValue);
        var operationalFact = Assert.IsType<AiOperationalSummaryFact>(operational.Data);
        Assert.Equal(1, operationalFact.ActiveOrders);
        Assert.Equal(1, operationalFact.PaidCompletedOrders);
        Assert.Equal(1, operationalFact.LowStockItems);
        Assert.Equal(1, operationalFact.UnavailableMenuItems);
        Assert.Equal(AiFunctionStatus.Unauthorized, waiter.Status);
    }

    [Fact]
    public async Task Invalid_ambiguous_and_unknown_requests_fail_closed_and_are_audited()
    {
        var scenario = await CreateScenarioAsync(duplicateBurger: true);

        var ambiguous = await scenario.Service.ExecuteAsync(
            new AiFunctionRequest(AiFunctionName.GetMenuItem, ItemName: "Cheeseburger"),
            scenario.Admin);
        var invalidOrder = await scenario.Service.ExecuteAsync(
            new AiFunctionRequest(
                AiFunctionName.GetOrderStatus,
                OrderId: scenario.PaidOrderId,
                TableNumber: "1"),
            scenario.Admin);
        var missingInventory = await scenario.Service.ExecuteAsync(
            new AiFunctionRequest(AiFunctionName.GetInventoryBalance, ItemName: "Dragon fruit"),
            scenario.Admin);

        Assert.Equal(AiFunctionStatus.Ambiguous, ambiguous.Status);
        Assert.Equal(AiFunctionStatus.InvalidRequest, invalidOrder.Status);
        Assert.Equal(AiFunctionStatus.NotFound, missingInventory.Status);
        await using var db = scenario.Factory.CreateDbContext();
        var audits = await db.AuditEntries.Where(x => x.Action.StartsWith("AiRead:"))
            .OrderBy(x => x.Id)
            .ToListAsync();
        Assert.Equal(3, audits.Count);
        Assert.All(audits, audit => Assert.DoesNotContain("prompt", audit.NewValuesJson!, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task List_functions_cap_results_at_the_protocol_limit()
    {
        var scenario = await CreateScenarioAsync();
        await using (var db = scenario.Factory.CreateDbContext())
        {
            var category = await db.MenuCategories.SingleAsync(x => x.Name == "Bulk");
            for (var index = 0; index < 105; index++)
                db.MenuItems.Add(new MenuItem
                {
                    CategoryId = category.Id,
                    Name = $"Bulk item {index:000}",
                    Price = index + 1,
                    IsAvailable = true
                });
            await db.SaveChangesAsync();
        }

        var response = await scenario.Service.ExecuteAsync(
            new AiFunctionRequest(AiFunctionName.ListMenu, Category: "Bulk"),
            scenario.Admin);

        Assert.Equal(AiFunctionProtocol.MaximumResults,
            Assert.IsAssignableFrom<IReadOnlyList<AiMenuItemFact>>(response.Data).Count);
    }

    [Fact]
    public async Task Assistant_coordinator_executes_only_a_validated_function()
    {
        var scenario = await CreateScenarioAsync();
        var gateway = new FakeGateway(request => new InterpretCommandResponse(
            RestaurantCommandProtocol.CurrentSchemaVersion,
            request.RequestId,
            InterpretationStatus.Recognized,
            new ValidatedCommandProposal(
                RestaurantCommandName.GetInventoryBalance,
                null,
                "Cooking oil",
                null,
                null,
                null,
                null,
                null,
                null),
            []));
        var assistant = CreateAssistant(scenario, gateway);

        var result = await assistant.AskAsync("How much Cooking oil is left?", scenario.Kitchen);

        Assert.Equal(AiAssistantStatus.Success, result.Status);
        Assert.Equal(AiFunctionName.GetInventoryBalance, result.Function);
        Assert.IsType<AiInventoryFact>(result.Data);
        Assert.NotNull(gateway.LastRequest);
        Assert.Contains(gateway.LastRequest!.Inventory, item => item.Name == "Cooking oil");
        Assert.Contains(RestaurantCommandName.GetInventoryBalance, gateway.LastRequest.AllowedCommands);
        Assert.Contains(gateway.LastRequest.Menu, item => item.Name == "Cheeseburger");
        Assert.Contains("1", gateway.LastRequest.TableNumbers);
    }

    [Fact]
    public async Task Assistant_coordinator_refuses_unsupported_interpretation_without_query_execution()
    {
        var scenario = await CreateScenarioAsync();
        var gateway = new FakeGateway(request => new InterpretCommandResponse(
            RestaurantCommandProtocol.CurrentSchemaVersion,
            request.RequestId,
            InterpretationStatus.Unsupported,
            null,
            ["Writes are unsupported."]));
        var assistant = CreateAssistant(scenario, gateway);

        var result = await assistant.AskAsync("Delete all inventory.", scenario.Admin);

        Assert.Equal(AiAssistantStatus.Unsupported, result.Status);
        await using var db = scenario.Factory.CreateDbContext();
        Assert.Empty(await db.AuditEntries.Where(x => x.Action.StartsWith("AiRead:")).ToListAsync());
        var attempt = Assert.Single(await db.AuditEntries
            .Where(x => x.Action.StartsWith("AiAssistant:"))
            .ToListAsync());
        Assert.DoesNotContain("Delete all inventory", attempt.NewValuesJson!);
        Assert.Contains("PromptSha256", attempt.NewValuesJson!);
    }

    [Fact]
    public async Task Assistant_filters_inventory_catalog_and_functions_before_waiter_gateway_call()
    {
        var scenario = await CreateScenarioAsync();
        var gateway = new FakeGateway(request => new InterpretCommandResponse(
            RestaurantCommandProtocol.CurrentSchemaVersion,
            request.RequestId,
            InterpretationStatus.Unsupported,
            null,
            ["Not permitted."]));
        var assistant = CreateAssistant(scenario, gateway);

        await assistant.AskAsync("How much Cooking oil is left?", scenario.WaiterOne);

        Assert.NotNull(gateway.LastRequest);
        Assert.Empty(gateway.LastRequest!.Inventory);
        Assert.DoesNotContain(RestaurantCommandName.GetInventoryBalance,
            gateway.LastRequest.AllowedCommands);
        Assert.Contains(RestaurantCommandName.GetMenuItem,
            gateway.LastRequest.AllowedCommands);
    }

    [Fact]
    public async Task Assistant_rate_limits_repeated_requests_per_user()
    {
        var scenario = await CreateScenarioAsync();
        var gateway = new FakeGateway(request => new InterpretCommandResponse(
            RestaurantCommandProtocol.CurrentSchemaVersion,
            request.RequestId,
            InterpretationStatus.Unsupported,
            null,
            ["Unsupported."]));
        var assistant = CreateAssistant(scenario, gateway, requestsPerMinute: 1);

        var first = await assistant.AskAsync("First harmless question", scenario.Admin);
        var second = await assistant.AskAsync("Second harmless question", scenario.Admin);

        Assert.Equal(AiAssistantStatus.Unsupported, first.Status);
        Assert.Equal(AiAssistantStatus.RateLimited, second.Status);
    }

    [Fact]
    public void Assistant_gate_bounds_global_concurrency()
    {
        using var gate = new AiRequestGate(Options.Create(new AiSecurityOptions
        {
            MaxConcurrentRequests = 1,
            RequestsPerMinute = 10
        }));
        var now = new DateTime(2026, 8, 2, 10, 0, 0, DateTimeKind.Utc);

        var first = gate.TryAcquire("admin", now);
        var blocked = gate.TryAcquire("kitchen", now);
        first.Lease!.Dispose();
        var acceptedAfterRelease = gate.TryAcquire("kitchen", now);

        Assert.Equal(AiRequestAdmissionStatus.Accepted, first.Status);
        Assert.Equal(AiRequestAdmissionStatus.CapacityReached, blocked.Status);
        Assert.Equal(AiRequestAdmissionStatus.Accepted, acceptedAfterRelease.Status);
        acceptedAfterRelease.Lease!.Dispose();
    }

    private static AiAssistantService CreateAssistant(
        Scenario scenario,
        ICommandGatewayClient gateway,
        int requestsPerMinute = 6) =>
        new(
            scenario.Factory,
            gateway,
            scenario.Service,
            scenario.Clock,
            new AiRequestGate(Options.Create(new AiSecurityOptions
            {
                MaxConcurrentRequests = 2,
                RequestsPerMinute = requestsPerMinute
            })));

    private static async Task<Scenario> CreateScenarioAsync(bool duplicateBurger = false)
    {
        var options = new DbContextOptionsBuilder<RomsDbContext>()
            .UseInMemoryDatabase($"roms-ai-functions-{Guid.NewGuid()}")
            .Options;
        var factory = new TestFactory(options);
        var clock = new FixedClock();
        await using var db = factory.CreateDbContext();

        var adminRole = Role(RomsRoles.Admin);
        var waiterRole = Role(RomsRoles.Waiter);
        var kitchenRole = Role(RomsRoles.Kitchen);
        var admin = User("ai-admin");
        var waiterOne = User("waiter-one");
        var waiterTwo = User("waiter-two");
        var kitchen = User("ai-kitchen");
        db.Roles.AddRange(adminRole, waiterRole, kitchenRole);
        db.Users.AddRange(admin, waiterOne, waiterTwo, kitchen);
        db.UserRoles.AddRange(
            UserRole(admin, adminRole),
            UserRole(waiterOne, waiterRole),
            UserRole(waiterTwo, waiterRole),
            UserRole(kitchen, kitchenRole));

        var menu = new MenuCategory { Name = "Mains", SortOrder = 1 };
        var bulk = new MenuCategory { Name = "Bulk", SortOrder = 2 };
        var burger = new MenuItem
        {
            Category = menu,
            Name = "Cheeseburger",
            Description = "Stored menu description",
            Price = 185m,
            IsAvailable = true
        };
        db.MenuCategories.AddRange(menu, bulk);
        db.MenuItems.AddRange(
            burger,
            new MenuItem { Category = menu, Name = "Cola", Price = 60m, IsAvailable = false });
        if (duplicateBurger)
            db.MenuItems.Add(new MenuItem { Category = menu, Name = "CHEESEBURGER", Price = 999m });

        var oil = new InventoryItem { Name = "Cooking oil", Unit = "ml", MinimumStock = 10m };
        var rice = new InventoryItem { Name = "Rice", Unit = "g", MinimumStock = 5m };
        db.InventoryItems.AddRange(oil, rice);
        db.StockMovements.AddRange(
            Movement(oil, 8m, "oil-opening", clock.UtcNow),
            Movement(rice, 20m, "rice-opening", clock.UtcNow));

        var tableOne = new RestaurantTable { Number = "1", SortOrder = 1 };
        var tableTwo = new RestaurantTable { Number = "2", SortOrder = 2 };
        var tableThree = new RestaurantTable { Number = "3", SortOrder = 3 };
        db.RestaurantTables.AddRange(tableOne, tableTwo, tableThree);

        var paid = CreateOrder(tableOne, waiterOne.UserName!, burger, clock.UtcNow);
        paid.TransitionTo(OrderStatus.Preparing, kitchen.UserName!, null, clock.UtcNow);
        paid.TransitionTo(OrderStatus.Ready, kitchen.UserName!, null, clock.UtcNow);
        paid.TransitionTo(OrderStatus.Completed, waiterOne.UserName!, null, clock.UtcNow);
        paid.ConfirmPayment(admin.UserName!, clock.UtcNow);

        var preparing = CreateOrder(tableTwo, waiterTwo.UserName!, burger, clock.UtcNow);
        preparing.TransitionTo(OrderStatus.Preparing, kitchen.UserName!, null, clock.UtcNow);

        var cancelled = CreateOrder(tableThree, waiterOne.UserName!, burger, clock.UtcNow);
        cancelled.TransitionTo(OrderStatus.Cancelled, waiterOne.UserName!, "Guest cancelled", clock.UtcNow);

        db.Orders.AddRange(paid, preparing, cancelled);
        await db.SaveChangesAsync();

        return new Scenario(
            factory,
            new AiFunctionService(factory, clock),
            clock,
            admin.UserName!,
            waiterOne.UserName!,
            waiterTwo.UserName!,
            kitchen.UserName!,
            paid.Id,
            preparing.Id);
    }

    private static Order CreateOrder(
        RestaurantTable table,
        string waiter,
        MenuItem item,
        DateTime now)
    {
        var order = new Order { Table = table, WaiterId = waiter, CreatedUtc = now };
        order.AddItem(item, 1, null, now);
        order.Submit(now);
        return order;
    }

    private static IdentityRole Role(string name) => new(name)
    {
        NormalizedName = name.ToUpperInvariant()
    };

    private static ApplicationUser User(string username) => new()
    {
        UserName = username,
        NormalizedUserName = username.ToUpperInvariant()
    };

    private static IdentityUserRole<string> UserRole(ApplicationUser user, IdentityRole role) => new()
    {
        UserId = user.Id,
        RoleId = role.Id
    };

    private static StockMovement Movement(
        InventoryItem item,
        decimal quantity,
        string key,
        DateTime now) => new()
    {
        InventoryItem = item,
        Type = StockMovementType.Receipt,
        QuantityDelta = quantity,
        Reason = "Opening count",
        IdempotencyKey = key,
        ActorId = "ai-admin",
        OccurredUtc = now
    };

    private sealed record Scenario(
        TestFactory Factory,
        AiFunctionService Service,
        FixedClock Clock,
        string Admin,
        string WaiterOne,
        string WaiterTwo,
        string Kitchen,
        Guid PaidOrderId,
        Guid PreparingOrderId);

    private sealed class TestFactory(DbContextOptions<RomsDbContext> options)
        : IDbContextFactory<RomsDbContext>
    {
        public RomsDbContext CreateDbContext() => new(options);
    }

    private sealed class FixedClock : IClock
    {
        public DateTime UtcNow => new(2026, 8, 2, 10, 0, 0, DateTimeKind.Utc);
    }

    private sealed class FakeGateway(
        Func<InterpretCommandRequest, InterpretCommandResponse> responseFactory)
        : ICommandGatewayClient
    {
        public InterpretCommandRequest? LastRequest { get; private set; }

        public Task<InterpretCommandResponse> InterpretAsync(
            InterpretCommandRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(responseFactory(request));
        }
    }
}
