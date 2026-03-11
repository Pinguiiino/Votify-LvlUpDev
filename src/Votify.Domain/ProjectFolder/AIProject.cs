namespace Votify.Domain.ProjectFolder
{
    public class AiProject : Project
    {
        public AiProject() { }

        public AiProject(string title, string eventId, string categoryId,
                         double criterionA, double criterionB, string? description = null)
            : base(title, eventId, categoryId, criterionA, criterionB, description) { }

        public override string ProjectType() => "AI";
    }
}