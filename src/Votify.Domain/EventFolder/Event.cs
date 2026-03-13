using System;
using Votify.Domain.CategoryFolder;

namespace Votify.Domain.EventFolder
{
    public abstract class Event
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int MaxProjects { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<Category> AssociatedCategories { get; set; }
        public string? Description { get; set; }

        protected Event() { }

        protected Event(string name, int maxProjects, DateTime startDate, List<Category> associatedCategories, string? description = null)
        {
            this.Id = Guid.NewGuid().ToString();
            this.Name = name;
            this.MaxProjects = maxProjects;
            this.StartDate = startDate;
            this.Description = description;
            this.AssociatedCategories = associatedCategories;
        }

        public virtual string Summary()
            => $"{Name} — hasta {MaxProjects} proyectos, desde {StartDate:d}";
    }
}
