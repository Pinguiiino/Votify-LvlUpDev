using Votify.Domain.CategoryFolder;
using Votify.Domain.Factory;

namespace Votify.Domain.VoteFolder
{
    public class VoteService
    {
        private readonly IVoteRepository _repository;
        private readonly IVotingSessionRepository _sessionRepo;
        private readonly ICategoryRepository _categoryRepo;

        public VoteService(IVoteRepository repository,
                           IVotingSessionRepository sessionRepo,
                           ICategoryRepository categoryRepo)
        {
            _repository = repository;
            _sessionRepo = sessionRepo;
            _categoryRepo = categoryRepo;
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

        public async Task CastTopNVotesAsync(string userId, string categoryId, string votingSessionId,
                                             List<(string ProjectId, int Position, string? Comment)> rankedProjects)
        {
            if (rankedProjects == null || rankedProjects.Count == 0)
                throw new ArgumentException("No se han proporcionado proyectos para votar.");

            var session = await _sessionRepo.GetByIdAsync(votingSessionId)
                ?? throw new InvalidOperationException("La sesión de votación no existe.");

            if (session.CategoryId != categoryId)
                throw new InvalidOperationException("La sesión no pertenece a la categoría indicada.");

            if (!session.IsOpen)
                throw new InvalidOperationException("La sesión de votación no está abierta.");

            if (session.RequireComments && rankedProjects.Any(r => string.IsNullOrWhiteSpace(r.Comment)))
                throw new InvalidOperationException("Esta votación exige un comentario en cada voto.");

            if (session.EvaluationType == EvaluationType.TopN)
            {
                var limit = session.TopN ?? 0;
                if (rankedProjects.Count > limit)
                    throw new InvalidOperationException($"No se pueden votar más de {limit} proyectos en esta votación.");
            }

            await _repository.RemoveByUserInCategoryAsync(userId, categoryId, votingSessionId);

            VoteCreator factory = session.VoterType == VoterType.Jury
                ? new ExpertVoteCreator()
                : new PublicVoteCreator();

            var nuevos = new List<Vote>();
            foreach (var r in rankedProjects)
            {
                var comentario = string.IsNullOrWhiteSpace(r.Comment) ? null : r.Comment.Trim();
                var voto = factory.Create(
                    votingSessionId: session.Id,
                    projectId: r.ProjectId,
                    userId: userId,
                    categoryId: categoryId,
                    topPosition: r.Position,
                    comment: comentario);
                voto.GenerateIntegrityHash();
                nuevos.Add(voto);
            }

            await _repository.AddRangeAsync(nuevos);
            await _repository.SaveChangesAsync();
        }

        public Task<List<Vote>> GetUserVotesAsync(string userId, string categoryId)
            => _repository.GetByUserIdAndCategoryAsync(userId, categoryId);

        public async Task<List<CommentDto>> GetCommentsByProjectAsync(string projectId)
        {
            var votes = await _repository.GetByProjectAsync(projectId);
            return votes
                .Where(v => !string.IsNullOrWhiteSpace(v.Comment))
                .OrderBy(v => v.TopPosition)
                .Select(v => new CommentDto(
                    v.Comment!,
                    v.TopPosition,
                    v is ExpertVote ? "Expert" : "Public"))
                .ToList();
        }

        public async Task<List<string>> GetVotesByUserInCategoryAsync(string userId, string categoryId)
        {
            var votes = await _repository.GetByUserIdAndCategoryAsync(userId, categoryId);
            return votes.Select(v => v.VotedProjectId).ToList();
        }
    }

    public record CommentDto(string Comment, int TopPosition, string VoteType);
}
