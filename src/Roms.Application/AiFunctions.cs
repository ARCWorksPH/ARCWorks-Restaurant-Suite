using Roms.Domain;

namespace Roms.Application.Ai;

public static class AiFunctionProtocol
{
    public const int CurrentVersion = 1;
    public const int MaximumResults = 100;
    public const string RestaurantTimeZone = "Asia/Manila";
}

public enum AiFunctionName
{
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

public enum AiFunctionStatus
{
    Success,
    NotFound,
    Ambiguous,
    Unauthorized,
    InvalidRequest,
    Unsupported
}

public sealed record AiFunctionRequest(
    AiFunctionName Function,
    string? ItemName = null,
    string? Category = null,
    bool? Available = null,
    Guid? OrderId = null,
    string? TableNumber = null,
    OrderStatus? OrderStatus = null,
    DateOnly? BusinessDate = null);

public sealed record AiFunctionResponse(
    int ProtocolVersion,
    AiFunctionName Function,
    AiFunctionStatus Status,
    string Message,
    object? Data = null);

public sealed record AiMenuItemFact(
    Guid Id,
    string Name,
    string Category,
    string Description,
    decimal? Price,
    string Currency,
    bool Available);

public sealed record AiInventoryFact(
    Guid Id,
    string Name,
    decimal CurrentStock,
    string Unit,
    decimal MinimumStock,
    bool IsLowStock);

public sealed record AiOrderItemFact(
    string Name,
    int Quantity,
    decimal? UnitPrice);

public sealed record AiOrderStatusFact(
    Guid OrderId,
    string TableNumber,
    OrderStatus Status,
    DateTime CreatedUtc,
    DateTime? SubmittedUtc,
    DateTime? CompletedUtc,
    DateTime UpdatedUtc,
    bool PaymentConfirmed,
    decimal? Total,
    IReadOnlyList<AiOrderItemFact> Items);

public sealed record AiDailyOrderSummaryFact(
    DateOnly BusinessDate,
    string TimeZone,
    int PaidCompletedOrders,
    int CancelledOrders,
    decimal PaidCompletedOrderValue,
    string Currency);

public sealed record AiOrderStatusSummaryFact(
    int Draft,
    int New,
    int Preparing,
    int Ready,
    int PendingPayment);

public sealed record AiLowStockSummaryFact(
    int LowStockCount,
    IReadOnlyList<AiInventoryFact> Items);

public sealed record AiMenuAvailabilitySummaryFact(
    int ActiveItems,
    int AvailableItems,
    int UnavailableItems);

public sealed record AiOperationalSummaryFact(
    DateOnly BusinessDate,
    string TimeZone,
    int ActiveOrders,
    int ReadyOrders,
    int PaidCompletedOrders,
    decimal PaidCompletedOrderValue,
    int LowStockItems,
    int UnavailableMenuItems,
    string Currency);

public interface IAiFunctionService
{
    Task<AiFunctionResponse> ExecuteAsync(
        AiFunctionRequest request,
        string actorUsername,
        CancellationToken cancellationToken = default);
}

public enum AiAssistantStatus
{
    Success,
    ClarificationRequired,
    Unsupported,
    Unauthorized,
    InvalidRequest,
    RateLimited,
    InterpreterUnavailable
}

public sealed record AiAssistantResponse(
    AiAssistantStatus Status,
    string Message,
    AiFunctionName? Function = null,
    object? Data = null);

public interface IAiAssistantService
{
    Task<AiAssistantResponse> AskAsync(
        string text,
        string actorUsername,
        CancellationToken cancellationToken = default);
}
