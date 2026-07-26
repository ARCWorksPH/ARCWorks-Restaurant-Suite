using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Roms.Application;

namespace Roms.Web.Realtime;

[Authorize]
public sealed class OrderHub : Hub;

public sealed class OrderEventBus
{
    public event Action<OrderEvent>? Changed;
    public void Publish(OrderEvent message) => Changed?.Invoke(message);
}

public sealed class SignalROrderEventPublisher(IHubContext<OrderHub> hub, OrderEventBus bus) : IOrderEventPublisher
{
    public async Task PublishAsync(OrderEvent message, CancellationToken cancellationToken = default)
    {
        bus.Publish(message);
        await hub.Clients.All.SendAsync("OrderChanged", message, cancellationToken);
    }
}
