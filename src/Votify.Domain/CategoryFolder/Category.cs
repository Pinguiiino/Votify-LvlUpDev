using System;
using System.Collections.Generic;
using Votify.Domain.ProjectFolder;
using Votify.Domain.VoteFolder;

namespace Votify.Domain.CategoryFolder;

public class Category
{
    public string Id { get; set; }
    public string EventId { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public bool AllowSelfVoting { get; set; } = false;

    public bool CombineResults { get; set; } = false;
    public double? JuryWeight { get; set; }
    public double? PublicWeight { get; set; }

    public virtual List<VotingSession> VotingSessions { get; set; } = new();
    public virtual List<ProjectCategory> ProjectCategories { get; set; } = new();

    public Category() { }

    public Category(string eventId, string name,
                    string? description = null, bool allowSelfVoting = false,
                    bool combineResults = false,
                    double? juryWeight = null, double? publicWeight = null)
    {
        Id = Guid.NewGuid().ToString();
        EventId = eventId;
        Name = name;
        Description = description;
        AllowSelfVoting = allowSelfVoting;
        CombineResults = combineResults;
        JuryWeight = juryWeight;
        PublicWeight = publicWeight;
    }
}
