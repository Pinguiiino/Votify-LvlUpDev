namespace Votify.Domain.ProjectFolder
{
    public class GeneralProject : Project
    {
        public GeneralProject() { }

        public GeneralProject(string title, string eventId, string? ownerId = null,
                              string? description = null, string? imageUrl = null)
            : base(title, eventId, ownerId, description, imageUrl) { }

        public override string ProjectType() => "General";
    }
}
