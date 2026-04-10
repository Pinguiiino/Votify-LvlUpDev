using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Votify.Domain.EventFolder;
using Votify.Domain.Factory;

namespace Votify.Domain.VoteFolder
{
    public class VoteService
    {
        private readonly IVoteRepository _repository;
        private readonly IVotingSessionRepository _sessionRepo;
        private readonly IEventRepository _eventRepo;

        public VoteService(IVoteRepository repository, IVotingSessionRepository sessionRepo, IEventRepository eventRepo)
        {
            _repository = repository;
            _sessionRepo = sessionRepo;
            _eventRepo = eventRepo;
        }

        // AÑADIDO: int topPosition como parámetro para saber qué posición del ranking le estamos dando
        public async Task<Vote> CastVoteAsync(string projectId, string categoryId, string eventId, string userId, int topPosition)
        {
            var session = await _sessionRepo.GetActiveSessionByEventAsync(eventId);
            if (session == null)
                throw new InvalidOperationException("No hay una sesión de votación abierta.");

            var eventData = await _eventRepo.GetByIdAsync(eventId);
            if (eventData == null)
                throw new InvalidOperationException("Evento no encontrado.");

            var currentVotes = await _repository.CountVotesByUserInCategoryAsync(userId, categoryId);
            if (currentVotes >= eventData.TopNProjectsAllowed)
                throw new InvalidOperationException("Límite de votos alcanzado.");

            var alreadyVoted = await _repository.HasUserVotedForProjectAsync(userId, projectId);
            if (alreadyVoted)
                throw new InvalidOperationException("Ya has votado por este proyecto.");

            var factory = new PublicVoteCreator();

            // CORREGIDO: Pasamos topPosition (un int) en lugar del 1.0 que daba error
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
