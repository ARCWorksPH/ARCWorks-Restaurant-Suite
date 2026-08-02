using Roms.Application.Commands;
using Roms.CommandGateway;

namespace Roms.CommandGateway.Tests;

public sealed class CommandProposalValidatorTests
{
    private readonly CommandProposalValidator validator = new();
    private readonly InterpretCommandRequest request = new(
        "test-request",
        "test",
        Enum.GetValues<RestaurantCommandName>()
            .Where(x => x != RestaurantCommandName.Unknown).ToList(),
        [
            new("eggs", "Eggs", "piece", ["egg"]),
            new("rice", "Rice", "g", ["white rice"])
        ],
        [
            new("burger", "Cheeseburger", "Burgers", ["cheese burger"]),
            new("cola", "Cola", "Drinks", [])
        ],
        ["1", "12"]);

    [Fact]
    public void Accepts_exact_inventory_lookup()
    {
        var result = validator.Validate(
            request with { Text = "How many eggs are left?" },
            Proposal(RestaurantCommandName.GetInventoryBalance, item: "egg"));

        Assert.Equal(InterpretationStatus.Recognized, result.Status);
        Assert.Equal("eggs", result.Proposal?.ItemKey);
        Assert.Equal("Eggs", result.Proposal?.ItemName);
    }

    [Fact]
    public void Accepts_exact_menu_lookup_and_list_filters()
    {
        var item = validator.Validate(
            request with { Text = "How much is the cheese burger?" },
            Proposal(RestaurantCommandName.GetMenuItem, item: "cheese burger"));
        var list = validator.Validate(
            request with { Text = "Which Burgers are available?" },
            Proposal(RestaurantCommandName.ListMenu, category: "Burgers", available: true));

        Assert.Equal(InterpretationStatus.Recognized, item.Status);
        Assert.Equal("burger", item.Proposal?.ItemKey);
        Assert.Equal(InterpretationStatus.Recognized, list.Status);
        Assert.Equal("Burgers", list.Proposal?.Category);
        Assert.True(list.Proposal?.Available);
    }

    [Fact]
    public void Rejects_catalog_item_not_named_by_user()
    {
        var result = validator.Validate(
            request with { Text = "What is the weather today?" },
            Proposal(RestaurantCommandName.GetInventoryBalance, item: "Eggs"));

        Assert.Equal(InterpretationStatus.ClarificationRequired, result.Status);
        Assert.Contains(result.Issues, issue => issue.Contains("does not explicitly name"));
    }

    [Fact]
    public void Rejects_invented_menu_filter()
    {
        var result = validator.Validate(
            request with { Text = "List the menu." },
            Proposal(RestaurantCommandName.ListMenu, category: "Burgers", available: true));

        Assert.Equal(InterpretationStatus.ClarificationRequired, result.Status);
    }

    [Fact]
    public void Accepts_table_order_lookup_but_rejects_unknown_table()
    {
        var accepted = validator.Validate(
            request with { Text = "What is happening with table 12?" },
            Proposal(RestaurantCommandName.GetOrderStatus, tableNumber: "12"));
        var rejected = validator.Validate(
            request with { Text = "What is happening with table 99?" },
            Proposal(RestaurantCommandName.GetOrderStatus, tableNumber: "99"));

        Assert.Equal(InterpretationStatus.Recognized, accepted.Status);
        Assert.Equal("12", accepted.Proposal?.TableNumber);
        Assert.Equal(InterpretationStatus.ClarificationRequired, rejected.Status);
    }

    [Fact]
    public void Accepts_real_order_status_and_rejects_invented_status()
    {
        var accepted = validator.Validate(
            request with { Text = "Which orders are Ready?" },
            Proposal(RestaurantCommandName.ListOrdersByStatus, status: "Ready"));
        var rejected = validator.Validate(
            request with { Text = "Which orders are Served?" },
            Proposal(RestaurantCommandName.ListOrdersByStatus, status: "Served"));

        Assert.Equal(InterpretationStatus.Recognized, accepted.Status);
        Assert.Equal(Roms.Domain.OrderStatus.Ready, accepted.Proposal?.Status);
        Assert.Equal(InterpretationStatus.ClarificationRequired, rejected.Status);
    }

    [Fact]
    public void Date_summary_requires_exact_user_supplied_ISO_date()
    {
        var accepted = validator.Validate(
            request with { Text = "Give me the summary for 2026-08-02." },
            Proposal(RestaurantCommandName.GetDailyOrderSummary, businessDate: "2026-08-02"));
        var invented = validator.Validate(
            request with { Text = "Give me today's summary." },
            Proposal(RestaurantCommandName.GetDailyOrderSummary, businessDate: "2026-08-02"));

        Assert.Equal(new DateOnly(2026, 8, 2), accepted.Proposal?.BusinessDate);
        Assert.Equal(InterpretationStatus.ClarificationRequired, invented.Status);
    }

    [Fact]
    public void No_argument_function_rejects_hidden_arguments()
    {
        var result = validator.Validate(
            request with { Text = "Show low stock." },
            Proposal(RestaurantCommandName.GetLowStockSummary, item: "Eggs"));

        Assert.Equal(InterpretationStatus.ClarificationRequired, result.Status);
    }

    [Fact]
    public void Unknown_command_is_never_executable()
    {
        var result = validator.Validate(request,
            Proposal(RestaurantCommandName.Unknown));

        Assert.Equal(InterpretationStatus.Unsupported, result.Status);
        Assert.Null(result.Proposal);
    }

    [Fact]
    public void Rejects_function_that_is_not_permitted_for_caller()
    {
        var waiterRequest = request with
        {
            Text = "How many eggs are left?",
            AllowedCommands = [RestaurantCommandName.GetMenuItem]
        };

        var result = validator.Validate(waiterRequest,
            Proposal(RestaurantCommandName.GetInventoryBalance, item: "Eggs"));

        Assert.Equal(InterpretationStatus.Unsupported, result.Status);
        Assert.Null(result.Proposal);
    }

    private static ModelCommandProposal Proposal(
        RestaurantCommandName command,
        string item = "",
        string category = "",
        bool? available = null,
        string orderId = "",
        string tableNumber = "",
        string status = "",
        string businessDate = "") =>
        new(command, item, category, available, orderId, tableNumber, status, businessDate);
}
