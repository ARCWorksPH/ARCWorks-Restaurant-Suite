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
    public void Accepts_receipt_with_catalog_unit_alias()
    {
        var result = validator.Validate(request with
            { Text = "Receive 20 kilograms of white rice." },
            new(RestaurantCommandName.InventoryReceive, "white rice", 20, "kilograms"));

        Assert.Equal(InterpretationStatus.Recognized, result.Status);
        Assert.Equal(RestaurantCommandName.InventoryReceive, result.Proposal?.Command);
        Assert.Equal(20m, result.Proposal?.Quantity);
        Assert.Equal("kg", result.Proposal?.Unit);
    }

    [Fact]
    public void Rejects_unknown_inventory_item()
    {
        var result = validator.Validate(request with
            { Text = "Receive 20 kg of rice." },
            new(RestaurantCommandName.InventoryReceive, "receive", 20, "kg"));

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
    public void Rejects_write_when_original_text_is_a_lookup()
    {
        var result = validator.Validate(request with { Text = "Egg stock" },
            new(RestaurantCommandName.InventoryReceive, "Eggs", 1, "piece"));

        Assert.Equal(InterpretationStatus.ClarificationRequired, result.Status);
        Assert.Contains(result.Issues, x => x.Contains("receipt verb"));
    }

    [Fact]
    public void Rejects_write_when_model_changes_quantity()
    {
        var result = validator.Validate(request with
            { Text = "Receive 20 kg of rice." },
            new(RestaurantCommandName.InventoryReceive, "Rice", 10, "kg"));

        Assert.Equal(InterpretationStatus.ClarificationRequired, result.Status);
        Assert.Contains(result.Issues, x => x.Contains("exactly match"));
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
