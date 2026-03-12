using Votify.Domain.ProjectFolder;

namespace Votify.Domain.Factory
{
    public class AiProjectCreator : ProjectCreator
    {
        public override Project Create(string title, string eventId,
                                       double criterionA, double criterionB, string? description = null)
            => new AiProject(title, eventId, criterionA, criterionB, description);
    }
}