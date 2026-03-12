using Votify.Domain.ProjectFolder;

namespace Votify.Domain.Factory
{
    public abstract class ProjectCreator
    {

        public abstract Project Create(string title, string eventId,
                                       double criterionA, double criterionB, string? description = null);
    }
}
