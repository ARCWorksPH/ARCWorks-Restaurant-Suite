using Roms.Domain;

namespace Roms.Domain.Tests;

public sealed class OrderTests
{
    private static readonly DateTime Now = new(2026, 7, 13, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Submit_requires_an_item()
    {
        var order = NewOrder();
        var error = Assert.Throws<DomainException>(() => order.Submit(Now));
        Assert.Contains("at least one", error.Message);
    }

    [Fact]
    public void Price_and_name_are_snapshotted_when_item_is_added()
    {
        var menuItem = new MenuItem { Name = "Burger", Price = 185m };
        var order = NewOrder();
        order.AddItem(menuItem, 2, "No onions", Now);
        menuItem.Name = "Changed"; menuItem.Price = 999m;
        Assert.Equal("Burger", order.Items[0].MenuItemName);
        Assert.Equal(370m, order.Total);
    }

    [Fact]
    public void Happy_path_enforces_each_status_transition()
    {
        var order = WithItem();
        order.Submit(Now);
        order.TransitionTo(OrderStatus.Preparing, "kitchen", null, Now.AddMinutes(1));
        order.TransitionTo(OrderStatus.Ready, "kitchen", null, Now.AddMinutes(5));
        order.TransitionTo(OrderStatus.Completed, "waiter", null, Now.AddMinutes(7));
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(5L, order.Version);
        Assert.Equal(4, order.StatusHistory.Count);
    }

    [Fact]
    public void Kitchen_cannot_skip_preparing()
    {
        var order = WithItem(); order.Submit(Now);
        Assert.Throws<DomainException>(() => order.TransitionTo(OrderStatus.Ready, "kitchen", null, Now));
    }

    [Fact]
    public void Kitchen_return_requires_reason_and_waiter_resubmission_keeps_history()
    {
        var order = WithItem();
        order.Submit(Now);
        Assert.Throws<DomainException>(() => order.TransitionTo(OrderStatus.ReturnedToWaiter, "kitchen", " ", Now.AddMinutes(1)));

        order.TransitionTo(OrderStatus.ReturnedToWaiter, "kitchen", "Missing side", Now.AddMinutes(1));
        Assert.Throws<DomainException>(() => order.Submit(Now.AddMinutes(2)));
        order.Submit(Now.AddMinutes(2), "Added side and corrected note");

        Assert.Equal(OrderStatus.New, order.Status);
        Assert.Equal(1, order.ResubmissionCount);
        Assert.Contains(order.StatusHistory, x => x.ToStatus == OrderStatus.ReturnedToWaiter && x.Reason == "Missing side");
        Assert.Contains(order.StatusHistory, x => x.FromStatus == OrderStatus.ReturnedToWaiter && x.ToStatus == OrderStatus.New && x.Reason == "Added side and corrected note");
    }

    [Fact]
    public void Preparation_target_is_snapshotted_from_item_minutes_and_quantity()
    {
        var order = NewOrder();
        order.AddItem(new MenuItem { Name = "Burger", Price = 100m, PreparationMinutes = 5 }, 2, null, Now);
        order.AddItem(new MenuItem { Name = "Chicken", Price = 100m, PreparationMinutes = 10 }, 1, null, Now);
        order.Submit(Now);
        order.TransitionTo(OrderStatus.Preparing, "kitchen", null, Now.AddMinutes(1));
        order.SetPreparationTarget(20, Now.AddMinutes(1));

        Assert.Equal(20, order.PreparationTargetMinutes);
        Assert.Equal(Now.AddMinutes(21), order.PreparationTargetDueUtc);
    }

    [Fact]
    public void Payment_can_only_be_confirmed_after_serving_and_only_once()
    {
        var order = WithItem();
        Assert.Throws<DomainException>(() => order.ConfirmPayment("admin", Now));
        order.Submit(Now);
        order.TransitionTo(OrderStatus.Preparing, "kitchen", null, Now.AddMinutes(1));
        order.TransitionTo(OrderStatus.Ready, "kitchen", null, Now.AddMinutes(2));
        order.TransitionTo(OrderStatus.Completed, "waiter", null, Now.AddMinutes(3));
        order.ConfirmPayment("admin", Now.AddMinutes(4));
        Assert.NotNull(order.PaymentConfirmedUtc);
        Assert.Throws<DomainException>(() => order.ConfirmPayment("admin", Now.AddMinutes(5)));
    }

    [Fact]
    public void Cancellation_requires_a_reason()
    {
        var order = WithItem(); order.Submit(Now);
        Assert.Throws<DomainException>(() => order.TransitionTo(OrderStatus.Cancelled, "waiter", " ", Now));
        order.TransitionTo(OrderStatus.Cancelled, "waiter", "Customer left", Now);
        Assert.Equal("Customer left", order.CancellationReason);
    }

    [Fact]
    public void Preparing_item_removal_requires_admin_and_records_revision()
    {
        var order = WithItem(); order.Submit(Now); order.TransitionTo(OrderStatus.Preparing, "kitchen", null, Now);
        var itemId = order.Items[0].Id;
        Assert.Throws<DomainException>(() => order.AmendRemoveItem(
            itemId, "waiter", "Customer changed mind", false, Now));
        order.AmendRemoveItem(
            itemId,
            "admin",
            "Approved correction",
            true,
            Now);
        Assert.True(order.Items[0].IsRemoved);
        Assert.Equal(2, order.Revision);
    }

    [Fact]
    public void Prepared_order_can_be_cancelled_with_a_reason()
    {
        var order = WithItem();
        order.Submit(Now);
        order.TransitionTo(OrderStatus.Preparing, "kitchen", null, Now);

        order.TransitionTo(OrderStatus.Cancelled, "admin", "Customer left", Now);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal("Customer left", order.CancellationReason);
    }

    [Fact]
    public void Inventory_loss_requires_review_and_cannot_be_reviewed_twice()
    {
        var request = InventoryLossRequest.Report(
            Guid.NewGuid(),
            InventoryLossType.Spoilage,
            2.5m,
            "Temperature excursion",
            "kitchen",
            "loss-1",
            Now);

        Assert.Equal(InventoryLossStatus.Pending, request.Status);
        request.Approve("admin", "Verified against cold-storage log", Now.AddMinutes(5));

        Assert.Equal(InventoryLossStatus.Approved, request.Status);
        Assert.Equal("admin", request.ReviewedBy);
        Assert.Throws<DomainException>(() =>
            request.Reject("admin", "Cannot reverse approval", Now.AddMinutes(6)));
    }

    [Fact]
    public void Inventory_loss_rejection_requires_a_reason()
    {
        var request = InventoryLossRequest.Report(
            Guid.NewGuid(), InventoryLossType.Waste, 1m, "Dropped", "kitchen", "loss-2", Now);

        Assert.Throws<DomainException>(() => request.Reject("admin", " ", Now.AddMinutes(1)));
        request.Reject("admin", "No supporting incident record", Now.AddMinutes(2));

        Assert.Equal(InventoryLossStatus.Rejected, request.Status);
    }

    [Fact]
    public void Physical_count_records_zero_or_nonzero_variance_and_rejects_invalid_values()
    {
        var itemId = Guid.NewGuid();
        var count = InventoryCountRecord.Record(
            itemId,
            10m,
            7.5m,
            "Closing count sheet 42",
            "admin",
            "count-1",
            Now);

        Assert.Equal(itemId, count.InventoryItemId);
        Assert.Equal(10m, count.LedgerQuantity);
        Assert.Equal(7.5m, count.CountedQuantity);
        Assert.Equal(-2.5m, count.Variance);
        Assert.Equal("Closing count sheet 42", count.Reason);

        var zeroVariance = InventoryCountRecord.Record(
            itemId, 7.5m, 7.5m, "Witnessed recount", "admin", "count-2", Now);
        Assert.Equal(0m, zeroVariance.Variance);

        Assert.Throws<DomainException>(() =>
            InventoryCountRecord.Record(itemId, 1m, -1m, "Bad count", "admin", "count-3", Now));
        Assert.Throws<DomainException>(() =>
            InventoryCountRecord.Record(itemId, 1m, 1m, " ", "admin", "count-4", Now));
    }

    private static Order NewOrder() => new() { TableId = Guid.NewGuid(), WaiterId = "waiter", CreatedUtc = Now };
    private static Order WithItem() { var order = NewOrder(); order.AddItem(new MenuItem { Name = "Rice", Price = 50m }, 1, null, Now); return order; }
}
