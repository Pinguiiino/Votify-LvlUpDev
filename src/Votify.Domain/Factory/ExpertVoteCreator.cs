using Votify.Domain.VoteFolder;

namespace Votify.Domain.Factory
{
    public class ExpertVoteCreator : VoteCreator
    {
        public override Vote Create(string votingSessionId, string projectId,
                                    string userId, string categoryId,
                                    int topPosition, string? comment = null)
            => new ExpertVote(votingSessionId, projectId, userId, categoryId, topPosition, comment);
    }
}





