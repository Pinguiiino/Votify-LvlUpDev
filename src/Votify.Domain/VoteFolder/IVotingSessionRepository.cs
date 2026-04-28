namespace Votify.Domain.VoteFolder
{
    public interface IVotingSessionRepository
    {
        Task<VotingSession?> GetByIdAsync(string id);
        Task<List<VotingSession>> GetByCategoryAsync(string categoryId);
        Task<List<VotingSession>> GetByEventAsync(string eventId);
        Task<List<VotingSession>> GetActiveByEventAsync(string eventId);
    }
}
