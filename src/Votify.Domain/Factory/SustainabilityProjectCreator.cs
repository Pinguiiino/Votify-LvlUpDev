using Votify.Domain.ProjectFolder;

namespace Votify.Domain.Factory
{
    public class SustainabilityProjectCreator : ProjectCreator
    {
        public override Project Create(string title, string eventId,
                                       double criterionA, double criterionB, string? description = null)
            => new SustainabilityProject(title, eventId, criterionA, criterionB, description);
    }
}





