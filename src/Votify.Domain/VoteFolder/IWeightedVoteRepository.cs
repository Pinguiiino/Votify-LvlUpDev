namespace Votify.Domain.VoteFolder;

public interface IWeightedVoteRepository
{
    Task<List<WeightedVote>> GetByUserAndSessionAsync(string userId, string votingSessionId);
    Task<List<WeightedVote>> GetBySessionIdsAsync(IEnumerable<string> sessionIds);
    Task RemoveByUserAndSessionAsync(string userId, string votingSessionId);
    Task AddRangeAsync(IEnumerable<WeightedVote> votes);
    Task SaveChangesAsync();
    Task<List<WeightedVote>> GetByProjectAsync(string projectId);
}