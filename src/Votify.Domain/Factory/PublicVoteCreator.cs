using Votify.Domain.VoteFolder;

namespace Votify.Domain.Factory
{
    public class PublicVoteCreator : VoteCreator
    {
        public override Vote Create(string votingSessionId, string projectId,
                                    string userId, string categoryId,
                                    double rawScore, string? comment = null)
            => new PublicVote(votingSessionId, projectId, userId, categoryId, rawScore, comment);
    }
}







