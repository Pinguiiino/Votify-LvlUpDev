using System;
using System.Collections.Generic;
using System.Text;
using Votify.Domain.VoteFolder;
using Microsoft.EntityFrameworkCore;

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

    }
}
