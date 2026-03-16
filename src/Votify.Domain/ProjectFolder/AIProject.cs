namespace Votify.Domain.ProjectFolder
{
    public class AiProject : Project
    {
        public AiProject() { }

        public AiProject(string title, string eventId, string? description = null)
            : base(title, eventId, description) { }

        public override string ProjectType() => "AI";
    }
}