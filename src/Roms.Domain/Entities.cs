namespace Roms.Domain;

public enum UserRole { Admin, Waiter, Kitchen, Manager }
public enum OrderStatus { Draft, New, ReturnedToWaiter, Preparing, Ready, Completed, Cancelled }
public enum TableStatus { Available, Occupied, Preparing, ReadyToServe, PendingPayment }
public enum StockMovementType { Receipt, Consumption, Adjustment, Reversal, Waste, Spoilage }
public enum InventoryLossType { Waste, Spoilage }
public enum InventoryLossStatus { Pending, Approved, Rejected }
public enum WorkflowTimerKind { OrderEntry, KitchenAcceptance, Preparation }
public enum AttendanceClosureKind { Manual, AutomaticScheduledLimit, AutomaticUnscheduledLimit }
public enum StaffProfileLifecycle { Draft, Approved, Archived }
public enum StaffAnnouncementPriority { Normal, Important, Urgent }

public sealed class WorkflowSettings
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int OrderEntryMinutes { get; private set; } = 15;
    public int KitchenAcceptanceMinutes { get; private set; } = 5;
    public DateTime UpdatedUtc { get; private set; } = DateTime.UtcNow;
    public string UpdatedBy { get; private set; } = "system";

    public void Update(int orderEntryMinutes, int kitchenAcceptanceMinutes, string actorId, DateTime utcNow)
    {
        if (orderEntryMinutes is < 1 or > 240) throw new DomainException("Order-entry time must be between 1 and 240 minutes.");
        if (kitchenAcceptanceMinutes is < 1 or > 120) throw new DomainException("Kitchen acceptance time must be between 1 and 120 minutes.");
        if (string.IsNullOrWhiteSpace(actorId)) throw new DomainException("A manager or administrator is required.");
        OrderEntryMinutes = orderEntryMinutes;
        KitchenAcceptanceMinutes = kitchenAcceptanceMinutes;
        UpdatedBy = actorId.Trim();
        UpdatedUtc = utcNow;
    }
}

public sealed class StaffSchedule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = "";
    public DateTime ScheduledStartUtc { get; private set; }
    public DateTime ScheduledEndUtc { get; private set; }
    public string Notes { get; private set; } = "";
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; private set; } = DateTime.UtcNow;

    public void SetSchedule(DateTime startUtc, DateTime endUtc, string? notes, DateTime utcNow)
    {
        if (endUtc <= startUtc) throw new DomainException("Scheduled end time must be after the start time.");
        if (endUtc - startUtc > TimeSpan.FromHours(24)) throw new DomainException("A staff schedule cannot exceed 24 hours.");
        if ((notes?.Trim().Length ?? 0) > 500) throw new DomainException("Schedule notes cannot exceed 500 characters.");
        ScheduledStartUtc = startUtc;
        ScheduledEndUtc = endUtc;
        Notes = notes?.Trim() ?? "";
        UpdatedUtc = utcNow;
    }
}

public sealed class StaffAnnouncement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Version { get; private set; } = 1;
    public string Title { get; private set; } = "";
    public string Body { get; private set; } = "";
    public StaffAnnouncementPriority Priority { get; private set; }
    public string? AudienceRole { get; private set; }
    public DateTime PublishUtc { get; private set; }
    public DateTime? ExpiresUtc { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public string UpdatedBy { get; private set; } = "";
    public DateTime UpdatedUtc { get; private set; } = DateTime.UtcNow;

    public void Configure(string title, string body, StaffAnnouncementPriority priority,
        string? audienceRole, DateTime publishUtc, DateTime? expiresUtc, string actorId, DateTime utcNow,
        bool isEdit = false)
    {
        title = title?.Trim() ?? "";
        body = body?.Trim() ?? "";
        audienceRole = string.IsNullOrWhiteSpace(audienceRole) ? null : audienceRole.Trim();
        if (title.Length is < 1 or > 120) throw new DomainException("Announcement title must be between 1 and 120 characters.");
        if (body.Length is < 1 or > 2000) throw new DomainException("Announcement body must be between 1 and 2000 characters.");
        if (expiresUtc is not null && expiresUtc <= publishUtc) throw new DomainException("Announcement expiry must be after publication.");
        if (string.IsNullOrWhiteSpace(actorId)) throw new DomainException("An announcement author is required.");
        if (isEdit) Version++;
        Title = title;
        Body = body;
        Priority = priority;
        AudienceRole = audienceRole;
        PublishUtc = publishUtc;
        ExpiresUtc = expiresUtc;
        UpdatedBy = actorId.Trim();
        UpdatedUtc = utcNow;
    }

    public void SetActive(bool active, string actorId, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(actorId)) throw new DomainException("An announcement author is required.");
        IsActive = active;
        UpdatedBy = actorId.Trim();
        UpdatedUtc = utcNow;
    }
}

public sealed class StaffAnnouncementReceipt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AnnouncementId { get; set; }
    public StaffAnnouncement Announcement { get; set; } = null!;
    public string UserId { get; set; } = "";
    public int AnnouncementVersion { get; set; }
    public DateTime? AcknowledgedUtc { get; private set; }
    public DateTime? DismissedUtc { get; private set; }

    public void Acknowledge(DateTime utcNow) => AcknowledgedUtc ??= utcNow;

    public void Dismiss(DateTime utcNow)
    {
        if (DismissedUtc is null) DismissedUtc = utcNow;
    }
}

public sealed class AttendanceRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = "";
    public Guid? StaffScheduleId { get; set; }
    public StaffSchedule? StaffSchedule { get; set; }
    public DateTime ClockInUtc { get; private set; }
    public DateTime? ClockOutUtc { get; private set; }
    public string? AdjustedBy { get; private set; }
    public DateTime? AdjustedUtc { get; private set; }
    public string? AdjustmentReason { get; private set; }
    public AttendanceClosureKind? ClosureKind { get; private set; }
    public bool RequiresManagerReview { get; private set; }
    public string? ReviewedBy { get; private set; }
    public DateTime? ReviewedUtc { get; private set; }
    public string? ReviewReason { get; private set; }
    public long Version { get; private set; }

    public static AttendanceRecord ClockIn(string userId, Guid? scheduleId, DateTime utcNow) => new()
        { UserId = userId, StaffScheduleId = scheduleId, ClockInUtc = utcNow };

    public void ClockOut(DateTime utcNow)
    {
        if (ClockOutUtc is not null) throw new DomainException("This attendance record is already clocked out.");
        if (utcNow <= ClockInUtc) throw new DomainException("Clock-out time must be after clock-in time.");
        ClockOutUtc = utcNow;
        ClosureKind = AttendanceClosureKind.Manual;
        Version++;
    }

    public void CloseAutomatically(DateTime clockOutUtc, bool hadScheduledEnd)
    {
        if (ClockOutUtc is not null) throw new DomainException("This attendance record is already clocked out.");
        if (clockOutUtc <= ClockInUtc) throw new DomainException("Automatic clock-out time must be after clock-in time.");
        ClockOutUtc = clockOutUtc;
        ClosureKind = hadScheduledEnd
            ? AttendanceClosureKind.AutomaticScheduledLimit
            : AttendanceClosureKind.AutomaticUnscheduledLimit;
        RequiresManagerReview = true;
        Version++;
    }

    public void ReviewAutomaticClosure(string reviewerId, string reason, DateTime utcNow)
    {
        if (!RequiresManagerReview) throw new DomainException("This attendance record does not require review.");
        if (string.IsNullOrWhiteSpace(reviewerId)) throw new DomainException("A reviewing manager is required.");
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("A review reason is required.");
        if (reason.Trim().Length > 500) throw new DomainException("A review reason cannot exceed 500 characters.");
        RequiresManagerReview = false;
        ReviewedBy = reviewerId.Trim();
        ReviewedUtc = utcNow;
        ReviewReason = reason.Trim();
        Version++;
    }

    public void Correct(DateTime clockInUtc, DateTime? clockOutUtc, string adminId, string reason, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("A correction reason is required.");
        if (reason.Trim().Length > 500) throw new DomainException("A correction reason cannot exceed 500 characters.");
        if (clockOutUtc is not null && clockOutUtc <= clockInUtc) throw new DomainException("Clock-out time must be after clock-in time.");
        ClockInUtc = clockInUtc;
        ClockOutUtc = clockOutUtc;
        AdjustedBy = adminId;
        AdjustedUtc = utcNow;
        AdjustmentReason = reason.Trim();
        Version++;
    }
}

public sealed class RestaurantTable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Number { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

public sealed class MenuCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public List<MenuItem> Items { get; set; } = [];
}

public sealed class MenuItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CategoryId { get; set; }
    public MenuCategory? Category { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Price { get; set; }
    public int PreparationMinutes { get; set; } = 5;
    public bool IsActive { get; set; } = true;
    public bool IsAvailable { get; set; } = true;
}

public sealed class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TableId { get; set; }
    public RestaurantTable? Table { get; set; }
    public string WaiterId { get; set; } = "";
    public OrderStatus Status { get; private set; } = OrderStatus.Draft;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? SubmittedUtc { get; private set; }
    public DateTime? CompletedUtc { get; private set; }
    public DateTime? PaymentConfirmedUtc { get; private set; }
    public string? PaymentConfirmedBy { get; private set; }
    public string? CancellationReason { get; private set; }
    public int ResubmissionCount { get; private set; }
    public int? PreparationTargetMinutes { get; private set; }
    public DateTime? PreparationTargetDueUtc { get; private set; }
    public int? OrderEntryTargetMinutes { get; private set; }
    public DateTime? OrderEntryStartedUtc { get; private set; }
    public DateTime? OrderEntryDueUtc { get; private set; }
    public int? KitchenAcceptanceTargetMinutes { get; private set; }
    public DateTime? KitchenAcceptanceStartedUtc { get; private set; }
    public DateTime? KitchenAcceptanceDueUtc { get; private set; }
    public int Revision { get; private set; } = 1;
    public long Version { get; private set; }
    public List<OrderItem> Items { get; set; } = [];
    public List<OrderStatusHistory> StatusHistory { get; set; } = [];
    public decimal Total => Items.Where(x => !x.IsRemoved).Sum(x => x.UnitPrice * x.Quantity);

    public void AddItem(MenuItem menuItem, int quantity, string? notes, DateTime utcNow)
    {
        if (Status != OrderStatus.Draft) throw new DomainException("Only draft orders can be edited directly.");
        AddItemCore(menuItem, quantity, notes, utcNow);
    }

    public void AmendAddItem(MenuItem menuItem, int quantity, string? notes, string actorId, string reason, DateTime utcNow)
    {
        EnsureAmendable(reason);
        AddItemCore(menuItem, quantity, notes, utcNow);
        RecordAmendment(actorId, reason, utcNow);
    }

    public void AmendRemoveItem(
        Guid itemId,
        string actorId,
        string reason,
        bool actorIsAdmin,
        DateTime utcNow)
    {
        EnsureAmendable(reason);
        if (Status == OrderStatus.Preparing && !actorIsAdmin)
            throw new DomainException("Only an administrator can remove an item after preparation begins.");
        var item = Items.SingleOrDefault(x => x.Id == itemId && !x.IsRemoved) ?? throw new DomainException("Order item not found.");
        item.IsRemoved = true;
        RecordAmendment(actorId, reason, utcNow);
    }

    private void AddItemCore(MenuItem menuItem, int quantity, string? notes, DateTime utcNow)
    {
        if (!menuItem.IsActive || !menuItem.IsAvailable) throw new DomainException("This menu item is unavailable.");
        if (quantity < 1 || quantity > 99) throw new DomainException("Quantity must be between 1 and 99.");
        if ((notes?.Trim().Length ?? 0) > 500) throw new DomainException("Special instructions cannot exceed 500 characters.");

        var normalizedNotes = notes?.Trim() ?? "";
        var existing = Items.SingleOrDefault(x =>
            !x.IsRemoved &&
            x.MenuItemId == menuItem.Id &&
            string.Equals(x.Notes, normalizedNotes, StringComparison.Ordinal));

        if (existing is not null)
        {
            if (existing.Quantity + quantity > 99)
                throw new DomainException("The quantity for one item line cannot exceed 99.");

            existing.Quantity += quantity;
            Touch(utcNow);
            return;
        }

        Items.Add(new OrderItem
        {
            OrderId = Id,
            MenuItemId = menuItem.Id,
            MenuItemName = menuItem.Name,
            UnitPrice = menuItem.Price,
            Quantity = quantity,
            Notes = normalizedNotes
        });
        Touch(utcNow);
    }

    private void EnsureAmendable(string reason)
    {
        if (Status is not (OrderStatus.New or OrderStatus.Preparing))
            throw new DomainException("Only New or Preparing orders can be amended.");
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("An amendment reason is required.");
        if (reason.Trim().Length > 500) throw new DomainException("An amendment reason cannot exceed 500 characters.");
    }

    public void RemoveDraftItem(Guid itemId, DateTime utcNow)
    {
        if (Status != OrderStatus.Draft) throw new DomainException("Submitted items require an amendment.");
        var item = Items.SingleOrDefault(x => x.Id == itemId) ?? throw new DomainException("Order item not found.");
        Items.Remove(item);
        Touch(utcNow);
    }

    public void Submit(DateTime utcNow, string? resubmissionNote = null)
    {
        if (Status is not (OrderStatus.Draft or OrderStatus.ReturnedToWaiter))
            throw new DomainException("Only a draft or returned order can be submitted.");
        if (Items.Count == 0) throw new DomainException("Add at least one item before submitting.");
        if (Status == OrderStatus.ReturnedToWaiter && string.IsNullOrWhiteSpace(resubmissionNote))
            throw new DomainException("A resubmission note is required for a returned order.");
        var previous = Status;
        if (previous == OrderStatus.ReturnedToWaiter) ResubmissionCount++;
        Status = OrderStatus.New;
        SubmittedUtc = utcNow;
        AddHistory(previous, OrderStatus.New, WaiterId, resubmissionNote, utcNow);
    }

    public void StartOrderEntryTimer(int minutes, DateTime utcNow)
    {
        if (Status != OrderStatus.Draft) throw new DomainException("The order-entry timer can only start for a draft.");
        if (minutes is < 1 or > 240) throw new DomainException("Order-entry time must be between 1 and 240 minutes.");
        OrderEntryTargetMinutes = minutes;
        OrderEntryStartedUtc ??= utcNow;
        OrderEntryDueUtc = OrderEntryStartedUtc.Value.AddMinutes(minutes);
        Touch(utcNow);
    }

    public void StartKitchenAcceptanceTimer(int minutes, DateTime utcNow)
    {
        if (Status != OrderStatus.New) throw new DomainException("The kitchen acceptance timer can only start for a submitted order.");
        if (minutes is < 1 or > 120) throw new DomainException("Kitchen acceptance time must be between 1 and 120 minutes.");
        KitchenAcceptanceTargetMinutes = minutes;
        KitchenAcceptanceStartedUtc = utcNow;
        KitchenAcceptanceDueUtc = utcNow.AddMinutes(minutes);
        Touch(utcNow);
    }

    public void ExtendTimer(WorkflowTimerKind kind, int additionalMinutes, string reason, DateTime utcNow)
    {
        if (additionalMinutes is < 1 or > 120) throw new DomainException("An extension must be between 1 and 120 minutes.");
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("An extension reason is required.");
        if (reason.Trim().Length > 500) throw new DomainException("An extension reason cannot exceed 500 characters.");
        DateTime? due = kind switch
        {
            WorkflowTimerKind.OrderEntry => OrderEntryDueUtc,
            WorkflowTimerKind.KitchenAcceptance => KitchenAcceptanceDueUtc,
            WorkflowTimerKind.Preparation => PreparationTargetDueUtc,
            _ => null
        };
        if (due is null) throw new DomainException("This timer has not started.");
        switch (kind)
        {
            case WorkflowTimerKind.OrderEntry: OrderEntryDueUtc = due.Value.AddMinutes(additionalMinutes); break;
            case WorkflowTimerKind.KitchenAcceptance: KitchenAcceptanceDueUtc = due.Value.AddMinutes(additionalMinutes); break;
            case WorkflowTimerKind.Preparation: PreparationTargetDueUtc = due.Value.AddMinutes(additionalMinutes); break;
        }
        Touch(utcNow);
    }

    public void SetPreparationTarget(int targetMinutes, DateTime utcNow)
    {
        if (Status != OrderStatus.Preparing) throw new DomainException("Preparation target can only be set when preparation begins.");
        if (targetMinutes < 1 || targetMinutes > 24 * 60) throw new DomainException("Preparation target must be between 1 and 1440 minutes.");
        PreparationTargetMinutes = targetMinutes;
        PreparationTargetDueUtc = utcNow.AddMinutes(targetMinutes);
        Touch(utcNow);
    }

    public void TransitionTo(
        OrderStatus next,
        string actorId,
        string? reason,
        DateTime utcNow)
    {
        if (next == OrderStatus.Cancelled)
        {
            if (Status is OrderStatus.Completed or OrderStatus.Cancelled) throw new DomainException("This order can no longer be cancelled.");
            if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("A cancellation reason is required.");
            if (reason.Trim().Length > 500) throw new DomainException("A cancellation reason cannot exceed 500 characters.");
            var previous = Status;
            Status = next;
            CancellationReason = reason.Trim();
            AddHistory(previous, next, actorId, CancellationReason, utcNow);
            return;
        }

        var expected = Status switch
        {
            OrderStatus.New => OrderStatus.Preparing,
            OrderStatus.Preparing => OrderStatus.Ready,
            OrderStatus.Ready => OrderStatus.Completed,
            _ => (OrderStatus?)null
        };
        if (next == OrderStatus.ReturnedToWaiter && Status == OrderStatus.New)
        {
            if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("A return reason is required.");
            if (reason.Trim().Length > 500) throw new DomainException("A return reason cannot exceed 500 characters.");
            Status = next;
            AddHistory(OrderStatus.New, next, actorId, reason, utcNow);
            return;
        }
        if (expected != next) throw new DomainException($"Invalid transition from {Status} to {next}.");
        var from = Status;
        Status = next;
        if (next == OrderStatus.Completed) CompletedUtc = utcNow;
        AddHistory(from, next, actorId, reason, utcNow);
    }

    public void RecordAmendment(string actorId, string reason, DateTime utcNow)
    {
        if (Status is OrderStatus.Draft or OrderStatus.Completed or OrderStatus.Cancelled)
            throw new DomainException("This order cannot be amended.");
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("An amendment reason is required.");
        Revision++;
        UpdatedUtc = utcNow;
        Version++;
    }

    public void ConfirmPayment(string actorId, DateTime utcNow)
    {
        if (Status != OrderStatus.Completed) throw new DomainException("Only a served order can have payment confirmed.");
        if (PaymentConfirmedUtc is not null) throw new DomainException("Payment has already been confirmed for this order.");
        PaymentConfirmedUtc = utcNow;
        PaymentConfirmedBy = actorId;
        Touch(utcNow);
    }

    private void AddHistory(OrderStatus from, OrderStatus to, string actorId, string? reason, DateTime utcNow)
    {
        StatusHistory.Add(new OrderStatusHistory
        {
            OrderId = Id, FromStatus = from, ToStatus = to, ActorId = actorId,
            Reason = reason?.Trim(), OccurredUtc = utcNow
        });
        Touch(utcNow);
    }

    private void Touch(DateTime utcNow) { UpdatedUtc = utcNow; Version++; }
}

public sealed class OrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public Guid MenuItemId { get; set; }
    public string MenuItemName { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string Notes { get; set; } = "";
    public bool IsRemoved { get; set; }
}

public sealed class OrderStatusHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public OrderStatus FromStatus { get; set; }
    public OrderStatus ToStatus { get; set; }
    public string ActorId { get; set; } = "";
    public string? Reason { get; set; }
    public DateTime OccurredUtc { get; set; }
}

public sealed class OrderTimerExtension
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public WorkflowTimerKind Kind { get; set; }
    public int AdditionalMinutes { get; set; }
    public int ExtensionCount { get; set; }
    public string Reason { get; set; } = "";
    public string ActorId { get; set; } = "";
    public DateTime RequestedUtc { get; set; }
}

public sealed class AuditEntry
{
    public long Id { get; set; }
    public string ActorId { get; set; } = "system";
    public string Action { get; set; } = "";
    public string EntityType { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public string? Reason { get; set; }
    public DateTime OccurredUtc { get; set; } = DateTime.UtcNow;
}

public sealed class IdempotencyRecord
{
    public string Key { get; set; } = "";
    public string Operation { get; set; } = "";
    public Guid ResourceId { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public sealed class InventoryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Unit { get; set; } = "unit";
    public decimal MinimumStock { get; set; }
    public bool IsActive { get; set; } = true;
    public List<StockMovement> Movements { get; set; } = [];
    public decimal CurrentStock => Movements.Sum(x => x.QuantityDelta);
}

public sealed class StockMovement
{
    public long Id { get; set; }
    public Guid InventoryItemId { get; set; }
    public InventoryItem? InventoryItem { get; set; }
    public StockMovementType Type { get; set; }
    public decimal QuantityDelta { get; set; }
    public string Reason { get; set; } = "";
    public Guid? OrderId { get; set; }
    public string IdempotencyKey { get; set; } = "";
    public string ActorId { get; set; } = "system";
    public DateTime OccurredUtc { get; set; }
}

public sealed class InventoryCountRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InventoryItemId { get; private set; }
    public InventoryItem? InventoryItem { get; set; }
    public decimal LedgerQuantity { get; private set; }
    public decimal CountedQuantity { get; private set; }
    public decimal Variance { get; private set; }
    public string Reason { get; private set; } = "";
    public string CountedBy { get; private set; } = "";
    public DateTime CountedUtc { get; private set; }
    public string IdempotencyKey { get; private set; } = "";

    public static InventoryCountRecord Record(
        Guid inventoryItemId,
        decimal ledgerQuantity,
        decimal countedQuantity,
        string reason,
        string actorId,
        string idempotencyKey,
        DateTime utcNow)
    {
        if (inventoryItemId == Guid.Empty) throw new DomainException("An inventory item is required.");
        if (countedQuantity < 0) throw new DomainException("Physical count cannot be negative.");
        if (countedQuantity > 99_999_999_999.999m) throw new DomainException("Physical count is too large.");
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("A physical-count reason is required.");
        if (reason.Trim().Length > 500) throw new DomainException("A physical-count reason cannot exceed 500 characters.");
        if (string.IsNullOrWhiteSpace(actorId)) throw new DomainException("A counting staff member is required.");
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 150)
            throw new DomainException("A valid physical-count key is required.");
        return new InventoryCountRecord
        {
            InventoryItemId = inventoryItemId,
            LedgerQuantity = ledgerQuantity,
            CountedQuantity = countedQuantity,
            Variance = countedQuantity - ledgerQuantity,
            Reason = reason.Trim(),
            CountedBy = actorId,
            CountedUtc = utcNow,
            IdempotencyKey = idempotencyKey
        };
    }
}

public sealed class InventoryLossRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InventoryItemId { get; set; }
    public InventoryItem? InventoryItem { get; set; }
    public InventoryLossType Type { get; private set; }
    public decimal Quantity { get; private set; }
    public string Reason { get; private set; } = "";
    public string ReportedBy { get; private set; } = "";
    public DateTime ReportedUtc { get; private set; }
    public InventoryLossStatus Status { get; private set; } = InventoryLossStatus.Pending;
    public string? ReviewedBy { get; private set; }
    public DateTime? ReviewedUtc { get; private set; }
    public string? ReviewReason { get; private set; }
    public string IdempotencyKey { get; set; } = "";

    public static InventoryLossRequest Report(
        Guid inventoryItemId,
        InventoryLossType type,
        decimal quantity,
        string reason,
        string actorId,
        string idempotencyKey,
        DateTime utcNow)
    {
        if (inventoryItemId == Guid.Empty) throw new DomainException("An inventory item is required.");
        if (!Enum.IsDefined(type)) throw new DomainException("Waste or spoilage type is invalid.");
        if (quantity <= 0) throw new DomainException("Waste or spoilage quantity must be greater than zero.");
        if (quantity > 99_999_999_999.999m) throw new DomainException("Waste or spoilage quantity is too large.");
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("A waste or spoilage reason is required.");
        if (reason.Trim().Length > 500) throw new DomainException("A waste or spoilage reason cannot exceed 500 characters.");
        if (string.IsNullOrWhiteSpace(actorId)) throw new DomainException("A reporting staff member is required.");
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 150)
            throw new DomainException("A valid loss-report key is required.");
        return new InventoryLossRequest
        {
            InventoryItemId = inventoryItemId,
            Type = type,
            Quantity = quantity,
            Reason = reason.Trim(),
            ReportedBy = actorId,
            ReportedUtc = utcNow,
            IdempotencyKey = idempotencyKey
        };
    }

    public void Approve(string reviewerId, string? reviewReason, DateTime utcNow)
    {
        EnsurePending();
        if (string.IsNullOrWhiteSpace(reviewerId)) throw new DomainException("An approving manager is required.");
        if ((reviewReason?.Trim().Length ?? 0) > 500) throw new DomainException("A review reason cannot exceed 500 characters.");
        Status = InventoryLossStatus.Approved;
        ReviewedBy = reviewerId;
        ReviewedUtc = utcNow;
        ReviewReason = reviewReason?.Trim();
    }

    public void Reject(string reviewerId, string reason, DateTime utcNow)
    {
        EnsurePending();
        if (string.IsNullOrWhiteSpace(reviewerId)) throw new DomainException("A reviewing manager is required.");
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("A rejection reason is required.");
        if (reason.Trim().Length > 500) throw new DomainException("A rejection reason cannot exceed 500 characters.");
        Status = InventoryLossStatus.Rejected;
        ReviewedBy = reviewerId;
        ReviewedUtc = utcNow;
        ReviewReason = reason.Trim();
    }

    private void EnsurePending()
    {
        if (Status != InventoryLossStatus.Pending)
            throw new DomainException("This loss report has already been reviewed.");
    }
}

public sealed class DomainException(string message) : InvalidOperationException(message);
