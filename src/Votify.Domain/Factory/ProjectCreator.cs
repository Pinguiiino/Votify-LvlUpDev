using Votify.Domain.ProjectFolder;

namespace Votify.Factory;


public abstract class ProjectCreator
{

    public abstract Project Create(string title, string eventId, string categoryId,
                                   double criterionA, double criterionB, string? description = null);


}
