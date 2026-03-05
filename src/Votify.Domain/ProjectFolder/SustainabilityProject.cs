namespace Votify.Domain.ProjectFolder;


public class SustainabilityProject : Project
{
    public SustainabilityProject() { }

    public SustainabilityProject(string title, string eventId, string categoryId,
                                  double criterionA, double criterionB, string? description = null)
        : base(title, eventId, categoryId, criterionA, criterionB, description) { }

    public override string ProjectType() => "Sustainability";
}






