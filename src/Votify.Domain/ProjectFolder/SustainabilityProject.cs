namespace Votify.Domain.ProjectFolder
{
    public class SustainabilityProject : Project
    {
        public SustainabilityProject() { }

        public SustainabilityProject(string title, string eventId, string? ownerId = null,
                                     string? description = null, string? imageUrl = null)
            : base(title, eventId, ownerId, description, imageUrl) { }

        public override string ProjectType() => "Sustainability";
    }
}
