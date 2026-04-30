using Votify.Domain.Factory;

namespace Votify.Domain.VoteFolder.Strategies;

public sealed class TopNVotingStrategy : IVotingStrategy
{
    private readonly IVoteRepository _voteRepo;

    public TopNVotingStrategy(IVoteRepository voteRepo)
        => _voteRepo = voteRepo;

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

        VoteCreator factory = session.VoterType == VoterType.Jury
            ? new ExpertVoteCreator()
            : new PublicVoteCreator();

        var nuevos = input.RankedProjects.Select(r =>
        {
            var voto = factory.Create(
                votingSessionId: session.Id,
                projectId:       r.ProjectId,
                userId:          input.UserId,
                categoryId:      input.CategoryId,
                topPosition:     r.Position,
                comment:         string.IsNullOrWhiteSpace(r.Comment) ? null : r.Comment.Trim());
            voto.GenerateIntegrityHash();
            return voto;
        }).ToList();

        await _voteRepo.AddRangeAsync(nuevos);
        await _voteRepo.SaveChangesAsync();
    }
}
