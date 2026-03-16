namespace Votify.Domain.VoteFolder;

public class PublicVote : Vote
{
    public double RawScore { get; set; }

    public PublicVote() { }

    public PublicVote(string votingSessionId, string projectId, string userId,
                      string categoryId, double rawScore, string? comment = null)
        : base(votingSessionId, projectId, userId, categoryId, comment)
    {
        RawScore = rawScore;
    }

    public override double NormalizedScore() => RawScore * 0.85;
}

