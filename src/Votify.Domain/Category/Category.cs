

using Votify.Domain.ProjectFolder;

namespace Votify.Domain.CategoryFolder;

public class Category
{
    public string Id { get; set; }
    public string EventId { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string? PrizeDescription { get; set; }


    public double WeightCriterionA { get; set; }
    public double WeightCriterionB { get; set; }


    public List<Project> Projects { get; set; } = new();

    public Category() { }

    public Category(string eventId, string name, double weightA, double weightB,
                    string? description = null, string? prizeDescription = null)
    {
        Id = Guid.NewGuid().ToString();
        EventId = eventId;
        Name = name;
        WeightCriterionA = weightA;
        WeightCriterionB = weightB;
        Description = description;
        PrizeDescription = prizeDescription;
    }
}