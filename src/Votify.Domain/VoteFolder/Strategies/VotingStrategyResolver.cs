namespace Votify.Domain.VoteFolder.Strategies;

public sealed class VotingStrategyResolver
{
    private readonly IReadOnlyDictionary<EvaluationType, IVotingStrategy> _strategies;

    public VotingStrategyResolver(IEnumerable<IVotingStrategy> strategies)
    {
        var dict = new Dictionary<EvaluationType, IVotingStrategy>();
        foreach (var s in strategies)
            dict[s.SupportedType] = s;
        _strategies = dict;
    }

    public IVotingStrategy Resolve(EvaluationType type)
        => _strategies.TryGetValue(type, out var strategy)
            ? strategy
            : throw new NotSupportedException(
                $"No hay ninguna estrategia registrada para el tipo de evaluación '{type}'.");
}
