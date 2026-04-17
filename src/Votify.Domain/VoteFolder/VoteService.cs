using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        // topPosition: posición del ranking (1 = mejor proyecto, 2 = siguiente, etc.)
        public async Task<Vote> CastVoteAsync(string projectId, string categoryId, string eventId, string userId, int topPosition)
        {
            var session = await _sessionRepo.GetActiveSessionByEventAsync(eventId);
            if (session == null)
                throw new InvalidOperationException("No hay una sesión de votación abierta.");

            // El límite de votos depende de la categoría (ya no del evento)
            var category = await _categoryRepo.GetByIdAsync(categoryId);
            if (category == null)
                throw new InvalidOperationException("Categoría no encontrada.");

            var currentVotes = await _repository.CountVotesByUserInCategoryAsync(userId, categoryId);
            if (currentVotes >= category.TopNProjectsAllowed)
                throw new InvalidOperationException("Límite de votos alcanzado en esta categoría.");

            var alreadyVoted = await _repository.HasUserVotedForProjectAsync(userId, projectId);
            if (alreadyVoted)
                throw new InvalidOperationException("Ya has votado por este proyecto.");

            var factory = new PublicVoteCreator();

            var vote = factory.Create(session.Id, projectId, userId, categoryId, topPosition);

            return await _repository.AddAsync(vote);
        }

        public async Task<List<string>> GetVotesByUserInCategoryAsync(string userId, string categoryId)
        {
            var votes = await _repository.GetByUserIdAndCategoryAsync(userId, categoryId);
            return votes.Select(v => v.VotedProjectId).ToList();
        }
    }
}
