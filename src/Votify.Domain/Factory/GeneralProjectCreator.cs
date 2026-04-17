using Votify.Domain.ProjectFolder;

namespace Votify.Domain.Factory
{
    public class GeneralProjectCreator : ProjectCreator
    {
        public override Project Create(string title, string eventId,
                               string? description = null,
                               string? imageUrl = null)
    => new GeneralProject(title, eventId, description, imageUrl);
    }
}

