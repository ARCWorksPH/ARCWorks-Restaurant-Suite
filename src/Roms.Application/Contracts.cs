using Roms.Domain;

namespace Roms.Application;

public static class RomsRoles
{
    public const string Admin = "Admin";
    public const string Waiter = "Waiter";
    public const string Kitchen = "Kitchen";
}

public interface IClock { DateTime UtcNow { get; } }

public sealed record OrderEvent(Guid OrderId, int Revision, long Version, DateTime OccurredUtc, string Kind);
public interface IOrderEventPublisher { Task PublishAsync(OrderEvent message, CancellationToken cancellationToken = default); }

public sealed record TableCard(Guid Id, string Number, TableStatus Status, Guid? ActiveOrderId, decimal Total,
    string? WaiterId, string? WaiterName);
public sealed record MenuItemChoice(Guid Id, string Name, string Category, decimal Price, string Description);
public sealed record OrderItemView(Guid Id, string Name, decimal UnitPrice, int Quantity, string Notes, bool IsRemoved,
    InventoryDisposition? RemovalInventoryDisposition);
public sealed record OrderView(Guid Id, Guid TableId, string TableNumber, string WaiterId, string WaiterName, OrderStatus Status,
    DateTime CreatedUtc, DateTime? SubmittedUtc, DateTime? CompletedUtc, DateTime? PaymentConfirmedUtc,
    int Revision, long Version, decimal Total, string? CancellationReason,
    InventoryDisposition? CancellationInventoryDisposition,
    string? InventoryOverrideReason, string? InventoryOverriddenBy, DateTime? InventoryOverrideUtc,
    IReadOnlyList<OrderItemView> Items);
public sealed record DashboardReport(decimal CompletedOrderValue, int OrderCount, decimal AverageOrderValue,
    IReadOnlyList<BestSeller> BestSellers);
public sealed record BestSeller(string Name, int Quantity, decimal Value);
public sealed record InventoryBalance(Guid Id, string Name, string Unit, decimal CurrentStock, decimal MinimumStock, bool IsLow);
public sealed record StockMovementView(
    long Id,
    Guid InventoryItemId,
    string InventoryItemName,
    string Unit,
    StockMovementType Type,
    decimal QuantityDelta,
    string Reason,
    string ActorId,
    DateTime OccurredUtc);
public sealed record InventoryCountView(
    Guid Id,
    Guid InventoryItemId,
    string InventoryItemName,
    string Unit,
    decimal LedgerQuantity,
    decimal CountedQuantity,
    decimal Variance,
    string Reason,
    string CountedBy,
    DateTime CountedUtc);
public sealed record InventoryLossRequestView(
    Guid Id,
    Guid InventoryItemId,
    string InventoryItemName,
    string Unit,
    InventoryLossType Type,
    decimal Quantity,
    string Reason,
    string ReportedBy,
    DateTime ReportedUtc,
    InventoryLossStatus Status,
    string? ReviewedBy,
    DateTime? ReviewedUtc,
    string? ReviewReason);
public enum InventoryReadinessStatus { Pass, Blocked, Manual }
public sealed record InventoryReadinessCheck(
    string Code,
    string Name,
    InventoryReadinessStatus Status,
    string Evidence);
public sealed record InventoryReadinessReport(
    DateTime EvaluatedUtc,
    bool InventoryEnabled,
    int ActiveInventoryItemCount,
    int ActiveMenuItemCount,
    IReadOnlyList<InventoryReadinessCheck> Checks)
{
    public bool TechnicalChecksPassed => Checks.All(x => x.Status != InventoryReadinessStatus.Blocked);
    public int BlockingIssueCount => Checks.Count(x => x.Status == InventoryReadinessStatus.Blocked);
}
public sealed record StaffMemberView(string Id, string Username, string DisplayName);
public sealed record StaffScheduleView(Guid Id, string UserId, string Username, string DisplayName,
    DateTime ScheduledStartUtc, DateTime ScheduledEndUtc, string Notes);
public sealed record AttendanceRecordView(Guid Id, string UserId, string Username, string DisplayName,
    Guid? ScheduleId, DateTime ClockInUtc, DateTime? ClockOutUtc, decimal Hours,
    string? AdjustmentReason, string? AdjustedBy);
public sealed record MyAttendanceView(AttendanceRecordView? OpenRecord,
    IReadOnlyList<StaffScheduleView> Schedules, IReadOnlyList<AttendanceRecordView> Records);
public sealed record AttendanceAdminView(IReadOnlyList<StaffScheduleView> Schedules,
    IReadOnlyList<AttendanceRecordView> Records, IReadOnlyList<AttendanceRecordView> Present);

public interface IOrderService
{
    Task<IReadOnlyList<TableCard>> GetTablesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MenuItemChoice>> GetMenuAsync(CancellationToken cancellationToken = default);
    Task<OrderView?> GetOrderAsync(Guid orderId, string actorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderView>> GetKitchenOrdersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderView>> GetPendingPaymentsAsync(CancellationToken cancellationToken = default);
    Task<Guid> GetOrCreateDraftAsync(Guid tableId, string waiterId, CancellationToken cancellationToken = default);
    Task AddItemAsync(Guid orderId, Guid menuItemId, int quantity, string? notes, string actorId, CancellationToken cancellationToken = default);
    Task RemoveDraftItemAsync(Guid orderId, Guid itemId, string actorId, CancellationToken cancellationToken = default);
    Task AmendAddItemAsync(Guid orderId, Guid menuItemId, int quantity, string? notes, string reason, string actorId,
        CancellationToken cancellationToken = default, bool allowNegativeStock = false, string? inventoryOverrideReason = null);
    Task AmendRemoveItemAsync(Guid orderId, Guid itemId, string reason, string actorId,
        CancellationToken cancellationToken = default, InventoryDisposition? inventoryDisposition = null);
    Task<Guid> SubmitAsync(Guid orderId, string idempotencyKey, string actorId, CancellationToken cancellationToken = default);
    Task TransitionAsync(Guid orderId, OrderStatus next, string actorId, string? reason = null,
        CancellationToken cancellationToken = default, InventoryDisposition? inventoryDisposition = null,
        bool allowNegativeStock = false, string? inventoryOverrideReason = null);
    Task ConfirmPaymentAsync(Guid orderId, string adminId, CancellationToken cancellationToken = default);
}

public interface ICatalogService
{
    Task<IReadOnlyList<RestaurantTable>> GetTablesAsync(CancellationToken cancellationToken = default);
    Task SaveTableAsync(RestaurantTable table, string actorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MenuCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task SaveCategoryAsync(MenuCategory category, string actorId, CancellationToken cancellationToken = default);
    Task SaveMenuItemAsync(MenuItem item, string actorId, CancellationToken cancellationToken = default);
}

public interface IReportService
{
    Task<DashboardReport> GetDashboardAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
}

public interface IInventoryService
{
    Task<IReadOnlyList<InventoryBalance>> GetBalancesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryItem>> GetItemsAsync(CancellationToken cancellationToken = default);
    Task SaveItemAsync(InventoryItem item, string actorId, CancellationToken cancellationToken = default);
    Task SetRecipeIngredientAsync(Guid menuItemId, Guid inventoryItemId, decimal quantity, string actorId, CancellationToken cancellationToken = default);
    Task ReceiveAsync(Guid itemId, decimal quantity, string deliveryReference, string? note, string actorId,
        string idempotencyKey, CancellationToken cancellationToken = default);
    Task<Guid> ReconcileCountAsync(Guid itemId, decimal countedQuantity, string reason, string actorId,
        string idempotencyKey, CancellationToken cancellationToken = default);
    Task AdjustAsync(Guid itemId, decimal delta, string reason, string actorId, string idempotencyKey,
        CancellationToken cancellationToken = default, bool allowNegativeStock = false, string? inventoryOverrideReason = null);
    Task<IReadOnlyList<StockMovementView>> GetRecentMovementsAsync(int take = 50, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryCountView>> GetRecentCountsAsync(int take = 25, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryLossRequestView>> GetLossRequestsAsync(CancellationToken cancellationToken = default);
    Task<Guid> ReportLossAsync(Guid itemId, InventoryLossType type, decimal quantity, string reason, string actorId,
        string idempotencyKey, CancellationToken cancellationToken = default);
    Task ReviewLossAsync(Guid requestId, bool approve, string? reviewReason, string adminId,
        CancellationToken cancellationToken = default);
    Task<InventoryReadinessReport> EvaluateReadinessAsync(string adminId,
        CancellationToken cancellationToken = default);
}

public interface IAttendanceService
{
    Task<MyAttendanceView> GetMineAsync(string username, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
    Task ClockInAsync(string username, CancellationToken cancellationToken = default);
    Task ClockOutAsync(string username, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StaffMemberView>> GetStaffAsync(CancellationToken cancellationToken = default);
    Task<AttendanceAdminView> GetAdminViewAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
    Task SaveScheduleAsync(Guid? scheduleId, string userId, DateTime startUtc, DateTime endUtc, string? notes, string adminId, CancellationToken cancellationToken = default);
    Task DeleteScheduleAsync(Guid scheduleId, string adminId, CancellationToken cancellationToken = default);
    Task CorrectAsync(Guid attendanceId, DateTime clockInUtc, DateTime? clockOutUtc, string reason, string adminId, CancellationToken cancellationToken = default);
}
