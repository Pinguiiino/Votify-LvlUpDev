using System;
using System.Collections.Generic;
using System.Linq;
using Votify.Domain.ProjectFolder;

namespace Votify.Domain.CategoryFolder;

public class Category
{
    public string Id { get; set; }
    public string EventId { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public bool AllowSelfVoting { get; set; } = false;
    public int TopNProjectsAllowed { get; set; } = 3;

    public virtual List<Criterion> Criteria { get; set; } = new();
    public virtual List<Prize> Prizes { get; set; } = new();
    public virtual List<ProjectCategory> ProjectCategories { get; set; } = new();

    public Category() { }

    public Category(string eventId, string name,
                    string? description = null, bool allowSelfVoting = false,
                    int topNProjectsAllowed = 3)
    {
        Id = Guid.NewGuid().ToString();
        EventId = eventId;
        Name = name;
        Description = description;
        AllowSelfVoting = allowSelfVoting;
        TopNProjectsAllowed = topNProjectsAllowed;
    }
}
