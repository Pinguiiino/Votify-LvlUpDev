using System;
using System.Collections.Generic;
using System.Text;

namespace Votify.Domain.VoteFolder
{
    public interface IVotingSessionRepository
    {
        Task<VotingSession?> GetByIdAsync(string id);
        Task<List<VotingSession>> GetByEventAsync(string eventId);
        Task<VotingSession?> GetActiveSessionByEventAsync(string eventId);
    }
}
