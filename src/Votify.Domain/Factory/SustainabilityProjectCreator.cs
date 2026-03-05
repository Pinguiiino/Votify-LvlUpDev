using Votify.Domain.ProjectFolder;

namespace Votify.Factory;


public class SustainabilityProjectCreator : ProjectCreator
{
    public override Project Create(string title, string eventId, string categoryId,
                                   double criterionA, double criterionB, string? description = null)
        => new SustainabilityProject(title, eventId, categoryId, criterionA, criterionB, description);
}





