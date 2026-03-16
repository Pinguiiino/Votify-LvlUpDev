using Votify.Domain.VoteFolder;

namespace Votify.Domain.Factory
{
    public class ExpertVoteCreator : VoteCreator
    {
        public override Vote Create(string votingSessionId, string projectId,
                                    string userId, string categoryId,
                                    double rawScore, string? comment = null)
            => new ExpertVote(votingSessionId, projectId, userId, categoryId, rawScore, comment);
    }
}





