using Votify.Domain.VoteFolder;

namespace Votify.Domain.Factory
{
    public class ExpertVoteCreator : VoteCreator
    {
        public override VoterType SupportedType => VoterType.Jury;

        public override Vote Create(string votingSessionId, string projectId,
                                    string userId, string categoryId,
                                    int topPosition, string? comment = null, int? points = null)
            => new ExpertVote(votingSessionId, projectId, userId, categoryId, topPosition, comment, points);
    }
}
