using System;
using System.Collections.Generic;
using Votify.Domain.CategoryFolder;

namespace Votify.Domain.VoteFolder;

public enum VoterType
{
    Jury,
    Public
}

public enum EvaluationType
{
    WeightedScale,
    PointDistribution,
    TopN
}

public class VotingSession
{
    public string Id { get; set; }
    public string CategoryId { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }

    public VoterType VoterType { get; set; }
    public EvaluationType EvaluationType { get; set; }
    public CriterionType? CriterionType { get; set; }

    public int? TopN { get; set; }
    public int? PointsPerVoter { get; set; }
    public int? MaxPointsPerProject { get; set; }

    public bool AllowComments { get; set; } = false;
    public bool RequireComments { get; set; } = false;
    public bool AllowCommentsPerCriterion { get; set; } = false;

    public DateTime OpenAt { get; set; }
    public DateTime CloseAt { get; set; }
    public DateTime? AdjustedCloseAt { get; set; }
    public DateTime EffectiveCloseAt => AdjustedCloseAt ?? CloseAt;
    public bool IsOpen => DateTime.UtcNow >= OpenAt && DateTime.UtcNow <= EffectiveCloseAt;
    public int? ReminderMinutesBeforeClose { get; set; }
    public bool IsManuallyAdjusted { get; set; } = false;
    public List<string> JurorEmails { get; set; } = new();
    public virtual List<Prize> Prizes { get; set; } = new();
    public virtual Category? Category { get; set; }
    public virtual List<Criterion> Criteria { get; set; } = new();
    public virtual List<Vote> Votes { get; set; } = new();

    public VotingSession() { }

    public VotingSession(string categoryId, string name,
                         VoterType voterType, EvaluationType evaluationType,
                         DateTime openAt, DateTime closeAt,
                         string? description = null,
                         CriterionType? criterionType = null,
                         int? topN = null,
                         int? pointsPerVoter = null,
                         int? maxPointsPerProject = null,
                         bool allowComments = false,
                         bool requireComments = false,
                         bool allowCommentsPerCriterion = false,
                         int? reminderMinutesBeforeClose = null)
    {
        Id = Guid.NewGuid().ToString();
        CategoryId = categoryId;
        Name = name;
        VoterType = voterType;
        EvaluationType = evaluationType;
        OpenAt = openAt;
        CloseAt = closeAt;
        Description = description;
        CriterionType = criterionType;
        TopN = topN;
        PointsPerVoter = pointsPerVoter;
        MaxPointsPerProject = maxPointsPerProject;
        AllowComments = allowComments;
        RequireComments = requireComments;
        AllowCommentsPerCriterion = allowCommentsPerCriterion;
        ReminderMinutesBeforeClose = reminderMinutesBeforeClose;
    }
}
