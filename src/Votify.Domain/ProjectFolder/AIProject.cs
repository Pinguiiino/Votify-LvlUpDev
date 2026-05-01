namespace Votify.Domain.ProjectFolder
{
    public class AiProject : Project
    {
        public AiProject() { }

        public AiProject(string title, string eventId, string? ownerId = null,
                         string? description = null, string? imageUrl = null)
            : base(title, eventId, ownerId, description, imageUrl) { }

        public override string ProjectType() => "AI";
    }
}
