namespace Votify.Domain.VoteFolder;

public class ExpertVote : Vote
{
    public double RawScore { get; set; }

    public ExpertVote() { }

    public ExpertVote(string votingSessionId, string projectId, string userId,
                      string categoryId, double rawScore, string? comment = null)
        : base(votingSessionId, projectId, userId, categoryId, comment)
    {
        RawScore = rawScore;
    }

    public override double NormalizedScore() => RawScore * 1.20;
}







