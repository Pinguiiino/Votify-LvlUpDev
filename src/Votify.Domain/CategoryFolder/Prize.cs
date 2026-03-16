using System;

namespace Votify.Domain.CategoryFolder;

public class Prize
{
    public string Id { get; set; }
    public string CategoryId { get; set; }
    public int Position { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }

    public virtual Category? Category { get; set; }

    public Prize() { }

    public Prize(string categoryId, int position, string name, string? description = null)
    {
        Id = Guid.NewGuid().ToString();
        CategoryId = categoryId;
        Position = position;
        Name = name;
        Description = description;
    }
}