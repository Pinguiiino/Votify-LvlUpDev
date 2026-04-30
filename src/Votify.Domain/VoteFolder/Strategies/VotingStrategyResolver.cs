namespace Votify.Domain.VoteFolder.Strategies;

public sealed class VotingStrategyResolver
{
    private readonly IReadOnlyDictionary<EvaluationType, IVotingStrategy> _strategies;

    public VotingStrategyResolver(IEnumerable<IVotingStrategy> strategies)
        => _strategies = strategies.ToDictionary(s => s.SupportedType);

    public IVotingStrategy Resolve(EvaluationType type)
        => _strategies.TryGetValue(type, out var strategy)
            ? strategy
            : throw new NotSupportedException(
                $"No hay ninguna estrategia registrada para el tipo de evaluación '{type}'.");
}
