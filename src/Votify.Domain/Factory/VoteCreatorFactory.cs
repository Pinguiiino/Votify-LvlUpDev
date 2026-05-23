using Votify.Domain.VoteFolder;

namespace Votify.Domain.Factory;

public sealed class VoteCreatorFactory
{
    private readonly IReadOnlyDictionary<VoterType, VoteCreator> _creators;

    public VoteCreatorFactory(IEnumerable<VoteCreator> creators)
        => _creators = creators.ToDictionary(c => c.SupportedType);

    public VoteCreator GetCreator(VoterType voterType)
        => _creators.TryGetValue(voterType, out var creator)
            ? creator
            : throw new NotSupportedException(
                $"No hay ningún VoteCreator registrado para el tipo '{voterType}'.");
}