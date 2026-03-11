using Votify.Domain.VoteFolder;

namespace Votify.Domain.Factory
{
    public class ExpertVoteCreator : VoteCreator
    {
        public override Vote Create(string projectId, string userId, double rawScore)
            => new ExpertVote(projectId, userId, rawScore);
    }
}






