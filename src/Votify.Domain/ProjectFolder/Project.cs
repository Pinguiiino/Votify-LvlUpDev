using Votify.Domain.CategoryFolder;

namespace Votify.Domain.ProjectFolder
{
    public abstract class Project
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public string EventId { get; set; }
        public virtual List<ProjectCategory> ProjectCategories { get; set; } = new List<ProjectCategory>();



        public double CriterionA { get; set; }
        public double CriterionB { get; set; }

        protected Project() { }

        protected Project(string title, string eventId,
                          double criterionA, double criterionB, string? description = null)
        {
            Id = Guid.NewGuid().ToString();
            Title = title;
            EventId = eventId;
            CriterionA = criterionA;
            CriterionB = criterionB;
            Description = description;
        }


        public abstract string ProjectType();


        public virtual double WeightedScore()
        {
            if (!ProjectCategories.Any())
                return (CriterionA + CriterionB) / 2.0;

            double total = 0;
            foreach(ProjectCategory category in ProjectCategories)
            {
                total += CriterionA * category.Category.WeightCriterionA + CriterionB * category.Category.WeightCriterionB;
            }
            return total / ProjectCategories.Count();

        }
    }
}