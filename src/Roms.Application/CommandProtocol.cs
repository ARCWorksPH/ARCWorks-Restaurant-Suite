using Roms.Domain;

namespace Roms.Application.Commands;

public static class RestaurantCommandProtocol
{
    public const int CurrentSchemaVersion = 4;
    public const int MaximumRequestLength = 500;
    public const int MaximumCatalogItems = 500;
}

public enum RestaurantCommandName
{
    Unknown,
    GetMenuItem,
    ListMenu,
    GetInventoryBalance,
    ListInventoryBalances,
    ListLowStockItems,
    GetOrderStatus,
    ListOrdersByStatus,
    GetDailyOrderSummary,
    GetOrderStatusSummary,
    GetLowStockSummary,
    GetMenuAvailabilitySummary,
    GetOperationalSummary
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
    IReadOnlyList<string> Aliases);

public sealed record MenuCatalogItem(
    string Key,
    string Name,
    string Category,
    IReadOnlyList<string> Aliases);

public sealed record InterpretCommandRequest(
    string RequestId,
    string Text,
    IReadOnlyList<RestaurantCommandName> AllowedCommands,
    IReadOnlyList<InventoryCatalogItem> Inventory,
    IReadOnlyList<MenuCatalogItem> Menu,
    IReadOnlyList<string> TableNumbers);

// This is untrusted model output. It must never be executed directly.
public sealed record ModelCommandProposal(
    RestaurantCommandName Command,
    string Item,
    string Category,
    bool? Available,
    string OrderId,
    string TableNumber,
    string Status,
    string BusinessDate);

public sealed record ValidatedCommandProposal(
    RestaurantCommandName Command,
    string? ItemKey,
    string? ItemName,
    string? Category,
    bool? Available,
    Guid? OrderId,
    string? TableNumber,
    OrderStatus? Status,
    DateOnly? BusinessDate);

public sealed record InterpretCommandResponse(
    int SchemaVersion,
    string RequestId,
    InterpretationStatus Status,
    ValidatedCommandProposal? Proposal,
    IReadOnlyList<string> Issues);

public interface ICommandGatewayClient
{
    Task<InterpretCommandResponse> InterpretAsync(
        InterpretCommandRequest request,
        CancellationToken cancellationToken = default);
}
