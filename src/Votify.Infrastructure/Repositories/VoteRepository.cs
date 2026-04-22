using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public Task AddRangeAsync(IEnumerable<Vote> votes)
        {
            foreach (var v in votes)
                _context.Votes.Add(v);
            return Task.CompletedTask;
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

        public async Task<List<Vote>> GetByUserAndCategoryOrderedAsync(string userId, string categoryId)
        {
            return await _context.Votes
                .Where(v => v.UserId == userId && v.CategoryId == categoryId)
                .OrderBy(v => v.TopPosition)
                .ToListAsync();
        }

        public async Task<List<Vote>> GetCommentsByProjectAsync(string projectId)
        {
            return await _context.Votes
                .Where(v => v.VotedProjectId == projectId && v.Comment != null && v.Comment != "")
                .OrderBy(v => v.TopPosition)
                .ToListAsync();
        }

        public Task RemoveRangeAsync(IEnumerable<Vote> votes)
        {
            _context.Votes.RemoveRange(votes);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
            => await _context.SaveChangesAsync();
    }
}