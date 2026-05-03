namespace Votify.Domain.VoteFolder;

public class VotingSessionService
{
    private readonly IVotingSessionRepository _repository;

    public VotingSessionService(IVotingSessionRepository repository)
    {
        _repository = repository;
    }

    public Task<List<VotingSession>> GetByEventAsync(string eventId)
        => _repository.GetByEventAsync(eventId);

    public Task<List<VotingSession>> GetActiveByEventAsync(string eventId)
        => _repository.GetActiveByEventAsync(eventId);

    public Task<VotingSession?> GetByIdAsync(string id)
        => _repository.GetByIdAsync(id);

    public Task<List<VotingSession>> GetByCategoryAsync(string categoryId)
        => _repository.GetByCategoryAsync(categoryId);

    public Task UpdateAsync(VotingSession session)
            => _repository.UpdateAsync(session);
}
