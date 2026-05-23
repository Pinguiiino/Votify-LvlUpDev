using Votify.Domain.Factory;

namespace Votify.Domain.VoteFolder.Strategies;

public sealed class PointDistributionVotingStrategy : IVotingStrategy
{
    private readonly IVoteRepository _voteRepo;
    private readonly VoteCreatorFactory _voteCreatorFactory;

    public PointDistributionVotingStrategy(
        IVoteRepository voteRepo,
        VoteCreatorFactory voteCreatorFactory)
    {
        _voteRepo = voteRepo;
        _voteCreatorFactory = voteCreatorFactory;
    }

    public EvaluationType SupportedType => EvaluationType.PointDistribution;

    public Task ValidateAsync(VotingSession session, VoteStrategyInput input)
    {
        var allocations = input.PointAllocations.Where(a => a.Points > 0).ToList();

        if (allocations.Count == 0)
            throw new InvalidOperationException("Debes asignar puntos a al menos un proyecto.");

        int budget = session.PointsPerVoter ?? 0;
        int maxPPP = session.MaxPointsPerProject ?? int.MaxValue;
        int total = allocations.Sum(a => a.Points);

        if (total > budget)
            throw new InvalidOperationException(
                $"No puedes repartir más de {budget} puntos en total. Te has pasado {total - budget}.");

        var excedido = allocations.FirstOrDefault(a => a.Points > maxPPP);
        if (excedido is not null)
            throw new InvalidOperationException(
                $"No puedes asignar más de {maxPPP} puntos a un mismo proyecto.");

        if (session.RequireComments &&
            allocations.Any(a => string.IsNullOrWhiteSpace(a.Comment)))
            throw new InvalidOperationException("El comentario es obligatorio en cada proyecto votado.");

        return Task.CompletedTask;
    }

    public async Task ExecuteAsync(VotingSession session, VoteStrategyInput input)
    {
        await _voteRepo.RemoveByUserInCategoryAsync(
            input.UserId, input.CategoryId, session.Id);

        var creator = _voteCreatorFactory.GetCreator(session.VoterType);

        var nuevos = input.PointAllocations
            .Where(a => a.Points > 0)
            .Select(a => BuildVote(creator, session, input, a))
            .ToList();

        await _voteRepo.AddRangeAsync(nuevos);
        await _voteRepo.SaveChangesAsync();
    }

    private static Vote BuildVote(
        VoteCreator creator, VotingSession session,
        VoteStrategyInput input, PointAllocationInput a)
    {
        var voto = creator.Create(
            votingSessionId: session.Id,
            projectId: a.ProjectId,
            userId: input.UserId,
            categoryId: input.CategoryId,
            topPosition: 0,
            comment: NormalizeComment(a.Comment),
            points: a.Points);
        voto.GenerateIntegrityHash();
        return voto;
    }

    private static string? NormalizeComment(string? raw)
        => string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();
}