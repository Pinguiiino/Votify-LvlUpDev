using Votify.Domain.Factory;

namespace Votify.Domain.VoteFolder.Strategies;

public sealed class TopNVotingStrategy : IVotingStrategy
{
    private readonly IVoteRepository _voteRepo;
    private readonly VoteCreatorFactory _voteCreatorFactory;

    public TopNVotingStrategy(
        IVoteRepository voteRepo,
        VoteCreatorFactory voteCreatorFactory)
    {
        _voteRepo = voteRepo;
        _voteCreatorFactory = voteCreatorFactory;
    }

    public EvaluationType SupportedType => EvaluationType.TopN;

    public Task ValidateAsync(VotingSession session, VoteStrategyInput input)
    {
        var ranked = input.RankedProjects;

        if (ranked.Count == 0)
            throw new InvalidOperationException("No se han proporcionado proyectos para votar.");

        var limit = session.TopN ?? 0;
        if (ranked.Count > limit)
            throw new InvalidOperationException(
                $"No se pueden votar más de {limit} proyectos en esta votación.");

        if (session.RequireComments && ranked.Any(r => string.IsNullOrWhiteSpace(r.Comment)))
            throw new InvalidOperationException("Esta votación exige un comentario en cada voto.");

        return Task.CompletedTask;
    }

    public async Task ExecuteAsync(VotingSession session, VoteStrategyInput input)
    {
        await _voteRepo.RemoveByUserInCategoryAsync(
            input.UserId, input.CategoryId, session.Id);

        var creator = _voteCreatorFactory.GetCreator(session.VoterType);

        var nuevos = input.RankedProjects
            .Select(r => BuildVote(creator, session, input, r))
            .ToList();

        await _voteRepo.AddRangeAsync(nuevos);
        await _voteRepo.SaveChangesAsync();
    }

    private static Vote BuildVote(
        VoteCreator creator, VotingSession session,
        VoteStrategyInput input, RankedProjectInput r)
    {
        var voto = creator.Create(
            votingSessionId: session.Id,
            projectId: r.ProjectId,
            userId: input.UserId,
            categoryId: input.CategoryId,
            topPosition: r.Position,
            comment: NormalizeComment(r.Comment));
        voto.GenerateIntegrityHash();
        return voto;
    }

    private static string? NormalizeComment(string? raw)
        => string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();
}
