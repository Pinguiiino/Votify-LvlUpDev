using System;
using System.Collections.Generic;
using System.Text;
using Votify.Domain.VoteFolder;
using Microsoft.EntityFrameworkCore;

namespace Votify.Infrastructure.Repositories
{
    public class VotingSessionRepository : IVotingSessionRepository
    {
        private readonly VotifyDbContext _context;

        public VotingSessionRepository(VotifyDbContext context) => _context = context;

        public async Task<VotingSession?> GetActiveSessionByEventAsync(string eventId)
        {
            var now = DateTime.UtcNow;
            return await _context.VotingSessions
                .FirstOrDefaultAsync(vs => vs.EventId == eventId &&
                                         now >= vs.OpenAt &&
                                         now <= (vs.AdjustedCloseAt ?? vs.CloseAt));
        }

        public async Task<List<VotingSession>> GetByEventAsync(string eventId) =>
            await _context.VotingSessions.Where(vs => vs.EventId == eventId).ToListAsync();

        public async Task<VotingSession?> GetByIdAsync(string id) =>
            await _context.VotingSessions.FindAsync(id);
    }
}
