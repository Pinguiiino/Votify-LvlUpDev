namespace Votify.Domain.VoteFolder.Strategies;

public sealed class WeightedVotingStrategy : IVotingStrategy
{
    private readonly IWeightedVoteRepository _weightedRepo;

    public WeightedVotingStrategy(IWeightedVoteRepository weightedRepo)
        => _weightedRepo = weightedRepo;

    public EvaluationType SupportedType => EvaluationType.WeightedScale;

    public Task ValidateAsync(VotingSession session, VoteStrategyInput input)
    {
        if (input.WeightedProjects.Count == 0)
            throw new InvalidOperationException("No se han proporcionado proyectos para evaluar.");

        if (session.RequireComments &&
            input.WeightedProjects.Any(e => string.IsNullOrWhiteSpace(e.Comment)))
            throw new InvalidOperationException("El comentario es obligatorio en cada proyecto.");

        return Task.CompletedTask;
    }

    public async Task ExecuteAsync(VotingSession session, VoteStrategyInput input)
    {
        await _weightedRepo.RemoveByUserAndSessionAsync(input.UserId, session.Id);

        var nuevos = input.WeightedProjects.Select(e =>
        {
            var wv = new WeightedVote(
                session.Id, e.ProjectId, input.UserId, input.CategoryId,
                string.IsNullOrWhiteSpace(e.Comment) ? null : e.Comment.Trim());

            foreach (var cs in e.CriterionScores)
            {
                var criterionComment = session.AllowCommentsPerCriterion && !string.IsNullOrWhiteSpace(cs.Comment)
                    ? cs.Comment.Trim()
                    : null;

                wv.CriterionScores.Add(
                    new WeightedCriterionScore(wv.Id, cs.CriterionId, Math.Clamp(cs.Score, 0, 10), criterionComment));
            }

            return wv;
        }).ToList();

        await _weightedRepo.AddRangeAsync(nuevos);
        await _weightedRepo.SaveChangesAsync();
    }
}
