namespace Votify.Domain.VoteFolder;

public class PublicVote : Vote
{
    public PublicVote() { }

    public PublicVote(string votingSessionId, string projectId, string userId,
                      string categoryId, int topPosition, string? comment = null)
        : base(votingSessionId, projectId, userId, categoryId, topPosition, comment)
    {
    }
}

