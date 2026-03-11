using Votify.Domain.CategoryFolder;

namespace Votify.Domain.ProjectFolder
{
    public abstract class Project
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public string EventId { get; set; }
        public string CategoryId { get; set; }


        public double CriterionA { get; set; }
        public double CriterionB { get; set; }


        public Category? Category { get; set; }

        protected Project() { }

        protected Project(string title, string eventId, string categoryId,
                          double criterionA, double criterionB, string? description = null)
        {
            Id = Guid.NewGuid().ToString();
            Title = title;
            EventId = eventId;
            CategoryId = categoryId;
            CriterionA = criterionA;
            CriterionB = criterionB;
            Description = description;
        }


        public abstract string ProjectType();


        public virtual double WeightedScore()
        {
            if (Category is not null)
                return CriterionA * Category.WeightCriterionA + CriterionB * Category.WeightCriterionB;

            return (CriterionA + CriterionB) / 2.0;
        }
    }
}