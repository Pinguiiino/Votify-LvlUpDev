using Votify.Domain.ProjectFolder;

namespace Votify.Domain.Factory
{
    public abstract class ProjectCreator
    {
        public abstract Project Create(string title, string eventId,
                                       string? description = null,
                                       string? imageUrl = null);
    }
}
