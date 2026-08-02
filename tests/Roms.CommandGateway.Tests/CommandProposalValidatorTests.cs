using Roms.Application.Commands;
using Roms.CommandGateway;

namespace Roms.CommandGateway.Tests;

public sealed class CommandProposalValidatorTests
{
    private readonly CommandProposalValidator validator = new();
    private readonly InterpretCommandRequest request = new(
        "test-request",
        "test",
        [
            new("eggs", "Eggs", "piece", ["egg"], ["piece", "pieces", "pc"]),
            new("rice", "Rice", "kg", ["white rice"], ["kg", "kilogram", "kilograms"])
        ]);

    [Fact]
    public void Accepts_exact_inventory_lookup()
    {
        var result = validator.Validate(request with { Text = "How many eggs are left?" },
            new(RestaurantCommandName.InventoryLookup, "egg", 0, ""));

        Assert.Equal(InterpretationStatus.Recognized, result.Status);
        Assert.Equal("eggs", result.Proposal?.ItemKey);
        Assert.Null(result.Proposal?.Quantity);
    }

    [Fact]
    public void Rejects_lookup_with_invented_quantity()
    {
        var result = validator.Validate(request with { Text = "How many eggs are left?" },
            new(RestaurantCommandName.InventoryLookup, "Eggs", 10, "none"));

        Assert.Equal(InterpretationStatus.ClarificationRequired, result.Status);
        Assert.Null(result.Proposal);
    }

    [Fact]
    public void Rejects_unknown_inventory_item()
    {
        var result = validator.Validate(request with
            { Text = "Receive 20 kg of rice." },
            new(RestaurantCommandName.InventoryLookup, "receive", 0, ""));

        Assert.Equal(InterpretationStatus.ClarificationRequired, result.Status);
        Assert.Null(result.Proposal);
    }

    [Fact]
    public void Unknown_command_is_never_executable()
    {
        var result = validator.Validate(request,
            new(RestaurantCommandName.Unknown, "", 0, ""));

        Assert.Equal(InterpretationStatus.Unsupported, result.Status);
        Assert.Null(result.Proposal);
    }

    [Fact]
    public void Rejects_catalog_item_not_named_by_user()
    {
        var result = validator.Validate(request with
            { Text = "What is the weather today?" },
            new(RestaurantCommandName.InventoryLookup, "Eggs", 0, ""));

        Assert.Equal(InterpretationStatus.ClarificationRequired, result.Status);
        Assert.Contains(result.Issues, x => x.Contains("does not explicitly name"));
    }
}
