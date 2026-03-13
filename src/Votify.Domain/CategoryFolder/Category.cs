
using Votify.Domain.ProjectFolder;

namespace Votify.Domain.CategoryFolder
{
    public class Category
    {
        public string Id { get; set; }
        public string EventId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? PrizeDescription { get; set; }


        public List<double>? Criterios { get; set; }

        public virtual List<ProjectCategory> ProjectCategories { get; set; } = new List<ProjectCategory>();

        public Category() { }

        public Category(string eventId, string name, List<double>? criterios = null,
                        string? description = null, string? prizeDescription = null)
        {
            this.Id = Guid.NewGuid().ToString();
            this.EventId = eventId;
            this.Name = name;
            this.Criterios = criterios;
            this.Description = description;
            this.PrizeDescription = prizeDescription;
        }
    }
}