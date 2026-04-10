using Votify.Domain.VoteFolder;

namespace Votify.Domain.Factory
{
    public abstract class VoteCreator
    {
        public abstract Vote Create(string votingSessionId, string projectId,
                                    string userId, string categoryId,
                                    int topPosition, string? comment = null);
    }
}






