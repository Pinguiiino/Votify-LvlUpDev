namespace Votify.Domain.ProjectFolder
{
    public class SustainabilityProject : Project
    {
        public SustainabilityProject() { }

        public SustainabilityProject(string title, string eventId, string? description = null)
            : base(title, eventId, description) { }

        public override string ProjectType() => "Sustainability";
    }
}






