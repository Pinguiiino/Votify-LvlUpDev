using Votify.Domain.ProjectFolder;

namespace Votify.Domain.Factory
{
    public class AiProjectCreator : ProjectCreator
    {
        public override Project Create(string title, string eventId,
                               string? description = null,
                               string? imageUrl = null)
    => new AiProject(title, eventId, description, imageUrl);
    }
}

