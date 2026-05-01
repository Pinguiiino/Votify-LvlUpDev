using Votify.Domain.ProjectFolder;

namespace Votify.Domain.Factory
{
    public class GeneralProjectCreator : ProjectCreator
    {
        public override Project Create(string title, string eventId,
                                       string? ownerId = null,
                                       string? description = null,
                                       string? imageUrl = null)
            => new GeneralProject(title, eventId, ownerId, description, imageUrl);
    }
}
