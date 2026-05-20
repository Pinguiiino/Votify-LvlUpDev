using Microsoft.AspNetCore.SignalR;

namespace Votify.Api.Hubs;

public class VoteHub : Hub
{
    public async Task JoinEventGroup(string eventId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"event-{eventId}");

    public async Task LeaveEventGroup(string eventId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"event-{eventId}");
}
