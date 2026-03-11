using Votify.Domain.VoteFolder;

namespace Votify.Domain.Factory
{
    public abstract class VoteCreator
    {
        public abstract Vote Create(string projectId, string userId, double rawScore);

    }
}






