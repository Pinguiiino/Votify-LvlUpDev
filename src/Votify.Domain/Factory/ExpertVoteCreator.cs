using Votify.Domain.VoteFolder;

namespace Votify.Factory;

/// <summary>
/// ConcreteCreator: fabrica votos de tipo Experto (Jury).
/// </summary>
public class ExpertVoteCreator : VoteCreator
{
    public override Vote Create(string projectId, string userId, double rawScore)
        => new ExpertVote(projectId, userId, rawScore);
}






