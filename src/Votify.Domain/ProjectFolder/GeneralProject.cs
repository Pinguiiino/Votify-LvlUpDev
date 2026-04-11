namespace Votify.Domain.ProjectFolder
{
    public class GeneralProject : Project
    {
        public GeneralProject() { }

        public GeneralProject(string title, string eventId, string? description = null)
            : base(title, eventId, description) { }

        public override string ProjectType() => "General";
    }
}
