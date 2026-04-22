
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Votify.Domain.VoteFolder
{
    public interface IVoteRepository
    {
        Task<Vote> AddAsync(Vote vote);
        Task AddRangeAsync(IEnumerable<Vote> votes);
        Task<List<Vote>> GetByUserIdAndCategoryAsync(string userId, string categoryId);
        Task<int> CountVotesByUserInCategoryAsync(string userId, string categoryId);
        Task<bool> HasUserVotedForProjectAsync(string userId, string projectId);
        Task<List<Vote>> GetByProjectAsync(string projectId);
        Task<List<Vote>> GetByUserAndCategoryOrderedAsync(string userId, string categoryId);
        Task<List<Vote>> GetCommentsByProjectAsync(string projectId);
        Task RemoveRangeAsync(IEnumerable<Vote> votes);
        Task SaveChangesAsync();
    }
}