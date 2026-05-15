using System;
using System.Collections.Generic;
using System.Linq;

namespace Votify.Domain.ProjectFolder;

public enum MaterialType
{
    Photo,
    Video,
    Document,
    Audio,
    Other
}

public enum ValidationStatus
{
    Pending,
    Approved,
    Rejected
}

public class ProjectMaterial
{
    public string Id { get; set; }
    public string ProjectId { get; set; }
    public MaterialType Type { get; set; }
    public string Url { get; set; }
    public string? Description { get; set; }

    public ProjectMaterial() { }

    public ProjectMaterial(string projectId, MaterialType type, string url,
                           string? description = null)
    {
        Id = Guid.NewGuid().ToString();
        ProjectId = projectId;
        Type = type;
        Url = url;
        Description = description;
    }
}

public abstract class Project
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public string EventId { get; set; }
    public string? OwnerId { get; set; }
    public string? ImageUrl { get; set; }

    public ValidationStatus ValidationStatus { get; set; } = ValidationStatus.Pending;
    public string? RejectionReason { get; set; }

    public virtual List<ProjectCategory> ProjectCategories { get; set; } = new();
    public virtual List<ProjectMaterial> Materials { get; set; } = new();

    protected Project() { }

    protected Project(string title, string eventId, string? ownerId = null,
                      string? description = null, string? imageUrl = null)
    {
        Id = Guid.NewGuid().ToString();
        Title = title;
        EventId = eventId;
        OwnerId = ownerId;
        Description = description;
        ImageUrl = imageUrl;
        ValidationStatus = ValidationStatus.Pending;
    }

    public abstract string ProjectType();

    public virtual double WeightedScore()
    {
        if (!ProjectCategories.Any()) return 0;
        return ProjectCategories.Average(pc => pc.WeightedScore());
    }
}
