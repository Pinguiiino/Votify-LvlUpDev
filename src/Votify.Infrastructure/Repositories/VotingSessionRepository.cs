using Microsoft.EntityFrameworkCore;
using Votify.Domain.VoteFolder;

namespace Votify.Infrastructure.Repositories
{
    public class VotingSessionRepository : IVotingSessionRepository
    {
        private readonly VotifyDbContext _context;

        public VotingSessionRepository(VotifyDbContext context) => _context = context;

        public async Task<VotingSession?> GetByIdAsync(string id)
            => await _context.VotingSessions.FindAsync(id);

        public async Task<List<VotingSession>> GetByCategoryAsync(string categoryId)
            => await _context.VotingSessions
                .Include(vs => vs.Criteria)
                .Where(vs => vs.CategoryId == categoryId)
                .ToListAsync();

        public async Task<List<VotingSession>> GetByEventAsync(string eventId)
            => await _context.VotingSessions
                .Include(vs => vs.Category)
                .Include(vs => vs.Criteria)
                .Where(vs => vs.Category != null && vs.Category.EventId == eventId)
                .ToListAsync();

        public async Task<List<VotingSession>> GetActiveByEventAsync(string eventId)
        {
            var now = DateTime.UtcNow;
            return await _context.VotingSessions
                .Include(vs => vs.Category)
                .Where(vs => vs.Category != null &&
                             vs.Category.EventId == eventId &&
                             now >= vs.OpenAt &&
                             now <= (vs.AdjustedCloseAt ?? vs.CloseAt))
                .ToListAsync();
        }
    }
}
