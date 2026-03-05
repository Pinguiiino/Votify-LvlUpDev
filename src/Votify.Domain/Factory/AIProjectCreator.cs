using Votify.Domain.ProjectFolder;

namespace Votify.Factory;


public class AiProjectCreator : ProjectCreator
{
    public override Project Create(string title, string eventId, string categoryId,
                                   double criterionA, double criterionB, string? description = null)
        => new AiProject(title, eventId, categoryId, criterionA, criterionB, description);
}