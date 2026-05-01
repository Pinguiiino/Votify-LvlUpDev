using Votify.Domain.ProjectFolder;

namespace Votify.Domain.Factory
{
    public abstract class ProjectCreator
    {
        public abstract Project Create(string title, string eventId,
                                       string? ownerId = null,
                                       string? description = null,
                                       string? imageUrl = null);
    }
}
