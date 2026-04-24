using System;
using Votify.Domain.VoteFolder;

namespace Votify.Domain.CategoryFolder;

public enum CriterionType
{
    Numeric,
    Checklist,
    Rubric
}

public class Criterion
{
    public string Id { get; set; }
    public string VotingSessionId { get; set; }
    public string Name { get; set; }
    public double Weight { get; set; }
    public string? Description { get; set; }

    public virtual VotingSession? VotingSession { get; set; }

    public Criterion() { }

    public Criterion(string votingSessionId, string name,
                     double weight, string? description = null)
    {
        Id = Guid.NewGuid().ToString();
        VotingSessionId = votingSessionId;
        Name = name;
        Weight = weight;
        Description = description;
    }
}
