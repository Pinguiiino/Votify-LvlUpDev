using Votify.Domain.VoteFolder;

namespace Votify.Domain.Factory
{
    public class PublicVoteCreator : VoteCreator
    {
        public override Vote Create(string projectId, string userId, double rawScore)
            => new PublicVote(projectId, userId, rawScore);
    }
}






