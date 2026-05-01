using Votify.Domain.ProjectFolder;

namespace Votify.Domain.Factory
{
    public class AiProjectCreator : ProjectCreator
    {
        public override Project Create(string title, string eventId,
                                       string? ownerId = null,
                                       string? description = null,
                                       string? imageUrl = null)
            => new AiProject(title, eventId, ownerId, description, imageUrl);
    }
}
