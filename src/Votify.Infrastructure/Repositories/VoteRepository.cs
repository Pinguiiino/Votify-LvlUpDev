using Microsoft.EntityFrameworkCore;
using Votify.Domain.VoteFolder;

namespace Votify.Infrastructure.Repositories
{
    public class VoteRepository : IVoteRepository
    {
        private readonly VotifyDbContext _context;

        public VoteRepository(VotifyDbContext context) => _context = context;

        public async Task<Vote> AddAsync(Vote vote)
        {
            _context.Votes.Add(vote);
            await _context.SaveChangesAsync();
            return vote;
        }

        public async Task AddRangeAsync(IEnumerable<Vote> votes)
        {
            await _context.Votes.AddRangeAsync(votes);
        }

        public async Task<int> CountVotesByUserInCategoryAsync(string userId, string categoryId)
        {
            return await _context.Votes
                .CountAsync(v => v.UserId == userId && v.CategoryId == categoryId);
        }

        public async Task<List<Vote>> GetByUserIdAndCategoryAsync(string userId, string categoryId)
        {
            return await _context.Votes
                .Where(v => v.UserId == userId && v.CategoryId == categoryId)
                .OrderBy(v => v.TopPosition)
                .ToListAsync();
        }

        public async Task<bool> HasUserVotedForProjectAsync(string userId, string projectId)
        {
            return await _context.Votes
                .AnyAsync(v => v.UserId == userId && v.VotedProjectId == projectId);
        }

        public async Task<List<Vote>> GetByProjectAsync(string projectId)
        {
            return await _context.Votes
                .Where(v => v.VotedProjectId == projectId)
                .ToListAsync();
        }

        public async Task<List<Vote>> GetByProjectIdsAsync(IEnumerable<string> projectIds)
        {
            var ids = projectIds.ToList();
            return await _context.Votes
                .AsNoTracking()
                .Where(v => ids.Contains(v.VotedProjectId))
                .ToListAsync();
        }

        public async Task RemoveByUserInCategoryAsync(string userId, string categoryId, string? votingSessionId = null)
        {
            var query = _context.Votes
                .Where(v => v.UserId == userId && v.CategoryId == categoryId);

            if (!string.IsNullOrWhiteSpace(votingSessionId))
                query = query.Where(v => v.VotingSessionId == votingSessionId);

            var previos = await query.ToListAsync();
            if (previos.Count > 0)
                _context.Votes.RemoveRange(previos);
        }

        public async Task SaveChangesAsync()
            => await _context.SaveChangesAsync();
    }
}
