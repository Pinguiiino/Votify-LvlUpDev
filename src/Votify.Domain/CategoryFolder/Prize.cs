using System;
using Votify.Domain.VoteFolder;

namespace Votify.Domain.CategoryFolder;

public class Prize
{
    public string Id { get; set; }
    public string VotingSessionId { get; set; }
    public int Position { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }

    public virtual VotingSession? VotingSession { get; set; }

    public Prize() { }

    public Prize(string votingSessionId, int position, string name, string? description = null)
    {
        Id = Guid.NewGuid().ToString();
        VotingSessionId = votingSessionId;
        Position = position;
        Name = name;
        Description = description;
    }
}
