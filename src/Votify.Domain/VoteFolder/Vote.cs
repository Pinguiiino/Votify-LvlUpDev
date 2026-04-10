using System;

namespace Votify.Domain.VoteFolder;
public class Vote
{
    public string Id { get; set; }
    public string VotingSessionId { get; set; }
    public string VotedProjectId { get; set; }
    public string UserId { get; set; }
    public string CategoryId { get; set; }
    public int TopPosition { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Comment { get; set; }

    public virtual VotingSession? VotingSession { get; set; }

    public Vote() { }

    public Vote(string votingSessionId, string projectId, string userId,
                string categoryId, int topPosition, string? comment = null)
    {
        Id = Guid.NewGuid().ToString();
        VotingSessionId = votingSessionId;
        VotedProjectId = projectId;
        UserId = userId;
        CategoryId = categoryId;
        TopPosition = topPosition;
        CreatedAt = DateTime.UtcNow;
        Comment = comment;
    }
}