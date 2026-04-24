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
        Task RemoveByUserInCategoryAsync(string userId, string categoryId, string? votingSessionId = null);
        Task SaveChangesAsync();
    }
}
