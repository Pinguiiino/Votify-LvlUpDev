using System;
using System.Collections.Generic;
using System.Text;

namespace Votify.Domain.VoteFolder;

public class WeightedVote
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string VotingSessionId { get; set; } = "";
    public string ProjectId { get; set; } = "";
    public string UserId { get; set; } = "";
    public string CategoryId { get; set; } = "";
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual List<WeightedCriterionScore> CriterionScores { get; set; } = new();

    public WeightedVote() { }

    public WeightedVote(string votingSessionId, string projectId,
                        string userId, string categoryId, string? comment = null)
    {
        VotingSessionId = votingSessionId;
        ProjectId = projectId;
        UserId = userId;
        CategoryId = categoryId;
        Comment = comment;
    }
}

public class WeightedCriterionScore
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string WeightedVoteId { get; set; } = "";
    public string CriterionId { get; set; } = "";
    public double Score { get; set; }
    public string? Comment { get; set; }

    public virtual WeightedVote? WeightedVote { get; set; }

    public WeightedCriterionScore() { }

    public WeightedCriterionScore(string weightedVoteId, string criterionId, double score, string? comment = null)
    {
        WeightedVoteId = weightedVoteId;
        CriterionId = criterionId;
        Score = score;
        Comment = comment;
    }
}
