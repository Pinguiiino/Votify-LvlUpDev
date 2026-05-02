using Votify.Domain.CategoryFolder;
using Votify.Domain.Factory;
using Votify.Domain.ProjectFolder;
using Votify.Domain.VoteFolder.Strategies;

namespace Votify.Domain.VoteFolder
{
    public class VoteService
    {
        private readonly IVoteRepository _repository;
        private readonly IVotingSessionRepository _sessionRepo;
        private readonly ICategoryRepository _categoryRepo;
        private readonly IProjectRepository _projectRepo;
        private readonly IWeightedVoteRepository _weightedRepo;
        private readonly VotingStrategyResolver _strategyResolver;

        public VoteService(IVoteRepository repository,
                           IVotingSessionRepository sessionRepo,
                           ICategoryRepository categoryRepo,
                           IProjectRepository projectRepo,
                           IWeightedVoteRepository weightedRepo,
                           VotingStrategyResolver strategyResolver)
        {
            _repository = repository;
            _sessionRepo = sessionRepo;
            _categoryRepo = categoryRepo;
            _projectRepo = projectRepo;
            _weightedRepo = weightedRepo;
            _strategyResolver = strategyResolver;
        }

        public async Task CastVotesByStrategyAsync(
        string votingSessionId,
        VoteStrategyInput input)
        {
            var session = await _sessionRepo.GetByIdAsync(votingSessionId)
                ?? throw new InvalidOperationException("La sesión de votación no existe.");

            if (session.CategoryId != input.CategoryId)
                throw new InvalidOperationException("La sesión no pertenece a la categoría indicada.");

            if (!session.IsOpen)
                throw new InvalidOperationException("La sesión de votación no está abierta.");

            await EnsureSelfVotingAllowedAsync(input);

            var strategy = _strategyResolver.Resolve(session.EvaluationType);
            await strategy.ValidateAsync(session, input);
            await strategy.ExecuteAsync(session, input);
        }

        private async Task EnsureSelfVotingAllowedAsync(VoteStrategyInput input)
        {
            var category = await _categoryRepo.GetByIdAsync(input.CategoryId);
            if (category == null || category.AllowSelfVoting)
                return;

            var projectIds = input.RankedProjects.Select(r => r.ProjectId)
                .Concat(input.WeightedProjects.Select(w => w.ProjectId))
                .Concat(input.PointAllocations.Where(p => p.Points > 0).Select(p => p.ProjectId))
                .Distinct()
                .ToList();

            foreach (var projectId in projectIds)
            {
                var project = await _projectRepo.GetByIdAsync(projectId);
                if (project != null && project.OwnerId == input.UserId)
                    throw new InvalidOperationException(
                        "No puedes votar tu propio proyecto en esta categoría.");
            }
        }

        public async Task<Vote> CastVoteAsync(string projectId, string categoryId, string eventId, string userId, int topPosition)
        {
            var activeSessions = await _sessionRepo.GetActiveByEventAsync(eventId);
            var session = activeSessions.FirstOrDefault(vs => vs.CategoryId == categoryId);
            if (session == null)
                throw new InvalidOperationException("No hay una sesión de votación abierta para esta categoría.");

            var category = await _categoryRepo.GetByIdAsync(categoryId);
            if (category == null)
                throw new InvalidOperationException("Categoría no encontrada.");

            if (!category.AllowSelfVoting)
            {
                var project = await _projectRepo.GetByIdAsync(projectId);
                if (project != null && project.OwnerId == userId)
                    throw new InvalidOperationException(
                        "No puedes votar tu propio proyecto en esta categoría.");
            }

            if (session.EvaluationType == EvaluationType.TopN)
            {
                var limit = session.TopN ?? 0;
                var currentVotes = await _repository.CountVotesByUserInCategoryAsync(userId, categoryId);
                if (currentVotes >= limit)
                    throw new InvalidOperationException("Límite de votos alcanzado en esta categoría.");
            }

            var alreadyVoted = await _repository.HasUserVotedForProjectAsync(userId, projectId);
            if (alreadyVoted)
                throw new InvalidOperationException("Ya has votado por este proyecto.");

            VoteCreator factory = session.VoterType == VoterType.Jury
                ? new ExpertVoteCreator()
                : new PublicVoteCreator();

            var vote = factory.Create(session.Id, projectId, userId, categoryId, topPosition);
            return await _repository.AddAsync(vote);
        }

        [Obsolete("Usa CastVotesByStrategyAsync con TopN input.")]
        public async Task CastTopNVotesAsync(
        string userId, string categoryId, string votingSessionId,
        List<(string ProjectId, int Position, string? Comment)> rankedProjects)
        {
            var input = new VoteStrategyInput
            {
                UserId = userId,
                CategoryId = categoryId,
                RankedProjects = rankedProjects
                    .Select(r => new RankedProjectInput(r.ProjectId, r.Position, r.Comment))
                    .ToArray()
            };
            await CastVotesByStrategyAsync(votingSessionId, input);
        }

        [Obsolete("Usa CastVotesByStrategyAsync con WeightedScale input.")]
        public async Task CastWeightedVotesAsync(
        string userId, string categoryId, string votingSessionId,
        List<(string ProjectId, string? Comment, List<(string CriterionId, double Score)> Scores)> projectEvals)
        {
            var input = new VoteStrategyInput
            {
                UserId = userId,
                CategoryId = categoryId,
                WeightedProjects = projectEvals.Select(e => new WeightedProjectInput(
                    e.ProjectId,
                    e.Comment,
                    e.Scores.Select(s => new CriterionScoreInput(s.CriterionId, s.Score)).ToArray()
                )).ToArray()
            };
            await CastVotesByStrategyAsync(votingSessionId, input);
        }

        public async Task<List<WeightedVoteDto>> GetWeightedVotesByUserAndSessionAsync(
            string userId, string votingSessionId)
        {
            var votes = await _weightedRepo.GetByUserAndSessionAsync(userId, votingSessionId);
            return votes.SelectMany(wv => wv.CriterionScores.Select(cs => new WeightedVoteDto(
                wv.ProjectId, cs.CriterionId, cs.Score, wv.Comment))).ToList();
        }

        public Task<List<Vote>> GetUserVotesAsync(string userId, string categoryId)
            => _repository.GetByUserIdAndCategoryAsync(userId, categoryId);

        public async Task<List<PointDistributionVoteDto>> GetPointDistributionVotesByUserAsync(
        string userId, string categoryId, string votingSessionId)
        {
            var votes = await _repository.GetByUserIdAndCategoryAsync(userId, categoryId);
            return votes
                .Where(v => v.VotingSessionId == votingSessionId)
                .Select(v => new PointDistributionVoteDto(v.VotedProjectId, v.Points ?? 0, v.Comment))
                .ToList();
        }

        public async Task<List<CommentDto>> GetCommentsByProjectAsync(string projectId)
        {
            var votes = await _repository.GetByProjectAsync(projectId);
            var sessionIds = votes.Select(v => v.VotingSessionId).Distinct().ToList();
            var sessions = new Dictionary<string, VotingSession>();
            foreach (var sid in sessionIds)
            {
                var s = await _sessionRepo.GetByIdAsync(sid);
                if (s != null) sessions[sid] = s;
            }
            return votes
                .Where(v => !string.IsNullOrWhiteSpace(v.Comment))
                .OrderBy(v => v.TopPosition)
                .Select(v =>
                {
                    var evalType = sessions.TryGetValue(v.VotingSessionId, out var s)
                        ? s.EvaluationType.ToString()
                        : "TopN";
                    var displayValue = s?.EvaluationType == EvaluationType.PointDistribution
                        ? v.Points ?? 0
                        : v.TopPosition;
                    return new CommentDto(
                        v.Comment!,
                        displayValue,
                        v is ExpertVote ? "Expert" : "Public",
                        evalType);
                })
                .ToList();
        }

        public async Task<List<string>> GetVotesByUserInCategoryAsync(string userId, string categoryId)
        {
            var votes = await _repository.GetByUserIdAndCategoryAsync(userId, categoryId);
            return votes.Select(v => v.VotedProjectId).ToList();
        }
    }

    public record WeightedVoteDto(string ProjectId, string CriterionId, double Score, string? Comment);
    public record PointDistributionVoteDto(string ProjectId, int Points, string? Comment);
    public record CommentDto(string Comment, int TopPosition, string VoteType, string EvaluationType);
}
