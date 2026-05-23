using Votify.Domain.VoteFolder;

namespace Votify.Domain.Factory
{
    public class PublicVoteCreator : VoteCreator
    {
        public override VoterType SupportedType => VoterType.Public;

        public override Vote Create(string votingSessionId, string projectId,
                                    string userId, string categoryId,
                                    int topPosition, string? comment = null, int? points = null)
            => new PublicVote(votingSessionId, projectId, userId, categoryId, topPosition, comment, points);
    }
}
