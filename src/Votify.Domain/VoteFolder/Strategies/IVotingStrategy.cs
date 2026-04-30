using Votify.Domain.VoteFolder;

namespace Votify.Domain.VoteFolder.Strategies;

public interface IVotingStrategy
{
    EvaluationType SupportedType { get; }
    Task ValidateAsync(VotingSession session, VoteStrategyInput input);
    Task ExecuteAsync(VotingSession session, VoteStrategyInput input);
}
