using System;
using Votify.Domain.ProjectFolder;
using Votify.Domain.UserFolder;

namespace Votify.Domain.VoteFolder;
public abstract class Vote
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string VotingSessionId { get; set; }
    public string VotedProjectId { get; set; }
    public string UserId { get; set; }
    public string CategoryId { get; set; }
    public int TopPosition { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? Comment { get; set; }

    public string IntegrityHash { get; set; }

    public void GenerateIntegrityHash()
    {
        var data = $"{Id}-{UserId}-{VotedProjectId}-{CreatedAt:O}";
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
        IntegrityHash = Convert.ToBase64String(bytes);
    }
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