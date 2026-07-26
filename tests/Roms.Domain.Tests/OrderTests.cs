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
        Assert.Throws<DomainException>(() => order.AmendRemoveItem(itemId, "waiter", "Customer changed mind", false, Now));
        order.AmendRemoveItem(itemId, "admin", "Approved correction", true, Now);
        Assert.True(order.Items[0].IsRemoved);
        Assert.Equal(2, order.Revision);
    }

    private static Order NewOrder() => new() { TableId = Guid.NewGuid(), WaiterId = "waiter", CreatedUtc = Now };
    private static Order WithItem() { var order = NewOrder(); order.AddItem(new MenuItem { Name = "Rice", Price = 50m }, 1, null, Now); return order; }
}
