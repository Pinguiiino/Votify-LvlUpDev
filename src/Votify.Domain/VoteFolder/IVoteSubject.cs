namespace Votify.Domain.VoteFolder;

public record VoteChangedEvent(string EventId, string CategoryId, string SessionId);

public interface IVoteSubject
{
    void RegisterObserver(IVoteObserver observer);
    void RemoveObserver(IVoteObserver observer);
    void NotifyObservers(VoteChangedEvent voteEvent);
}
