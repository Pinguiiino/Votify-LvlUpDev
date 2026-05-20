namespace Votify.Domain.VoteFolder;

public interface IVoteObserver
{
    Task Update(VoteChangedEvent voteEvent);
}
