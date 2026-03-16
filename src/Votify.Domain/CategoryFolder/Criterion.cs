using System;

namespace Votify.Domain.CategoryFolder;

public enum CriterionType
{
    Numeric,
    Checklist,
    Rubric,
    Comment,
    Audio,
    Video
}

public class Criterion
{
    public string Id { get; set; }
    public string CategoryId { get; set; }
    public string Name { get; set; }
    public CriterionType Type { get; set; }
    public double Weight { get; set; }
    public string? Description { get; set; }

    public virtual Category? Category { get; set; }

    public Criterion() { }

    public Criterion(string categoryId, string name, CriterionType type,
                     double weight, string? description = null)
    {
        Id = Guid.NewGuid().ToString();
        CategoryId = categoryId;
        Name = name;
        Type = type;
        Weight = weight;
        Description = description;
    }
}
