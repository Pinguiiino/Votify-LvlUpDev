using Votify.Domain.ProjectFolder;

namespace Votify.Domain.Factory
{
    public class SustainabilityProjectCreator : ProjectCreator
    {
        public override Project Create(string title, string eventId,
                               string? description = null,
                               string? imageUrl = null)
    => new SustainabilityProject(title, eventId, description, imageUrl);
    }
}






