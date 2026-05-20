using Microsoft.AspNetCore.SignalR;
using Votify.Api.Hubs;
using Votify.Domain.VoteFolder;

namespace Votify.Api.Services;

public class VoteNotifier : IVoteSubject
{
    private readonly IHubContext<VoteHub> _hubContext;

    public VoteNotifier(IHubContext<VoteHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public void RegisterObserver(IVoteObserver observer) { }

    public void RemoveObserver(IVoteObserver observer) { }

    public void NotifyObservers(VoteChangedEvent voteEvent)
        => _ = _hubContext.Clients
            .Group($"event-{voteEvent.EventId}")
            .SendAsync("VoteCast", voteEvent.EventId, voteEvent.CategoryId, voteEvent.SessionId);
}
