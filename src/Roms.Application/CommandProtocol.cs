namespace Roms.Application.Commands;

public static class RestaurantCommandProtocol
{
    public const int CurrentSchemaVersion = 2;
    public const int MaximumRequestLength = 500;
    public const int MaximumCatalogItems = 500;
}

public enum RestaurantCommandName
{
    Unknown,
    InventoryLookup
}

public enum InterpretationStatus
{
    Recognized,
    ClarificationRequired,
    Unsupported,
    InterpreterError
}

public sealed record InventoryCatalogItem(
    string Key,
    string Name,
    string Unit,
    IReadOnlyList<string> Aliases,
    IReadOnlyList<string> AcceptedUnits);

public sealed record InterpretCommandRequest(
    string RequestId,
    string Text,
    IReadOnlyList<InventoryCatalogItem> Inventory);

// This is untrusted model output. It must never be executed directly.
public sealed record ModelCommandProposal(
    RestaurantCommandName Command,
    string Item,
    decimal Quantity,
    string Unit);

public sealed record ValidatedCommandProposal(
    RestaurantCommandName Command,
    string ItemKey,
    string ItemName,
    decimal? Quantity,
    string? Unit);

public sealed record InterpretCommandResponse(
    int SchemaVersion,
    string RequestId,
    InterpretationStatus Status,
    ValidatedCommandProposal? Proposal,
    IReadOnlyList<string> Issues);
