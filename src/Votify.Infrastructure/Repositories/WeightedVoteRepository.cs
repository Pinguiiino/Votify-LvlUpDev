using Microsoft.EntityFrameworkCore;
using Votify.Domain.VoteFolder;

namespace Votify.Infrastructure.Repositories;

public class WeightedVoteRepository : IWeightedVoteRepository
{
    private readonly VotifyDbContext _ctx;
    public WeightedVoteRepository(VotifyDbContext ctx) => _ctx = ctx;

    public async Task<List<WeightedVote>> GetByUserAndSessionAsync(string userId, string votingSessionId)
        => await _ctx.WeightedVotes
            .Include(wv => wv.CriterionScores)
            .Where(wv => wv.UserId == userId && wv.VotingSessionId == votingSessionId)
            .ToListAsync();

    public async Task<List<WeightedVote>> GetBySessionIdsAsync(IEnumerable<string> sessionIds)
    {
        var ids = sessionIds.ToList();
        return await _ctx.WeightedVotes
            .Include(wv => wv.CriterionScores)
            .Where(wv => ids.Contains(wv.VotingSessionId))
            .ToListAsync();
    }

    public async Task RemoveByUserAndSessionAsync(string userId, string votingSessionId)
    {
        var existing = await _ctx.WeightedVotes
            .Where(wv => wv.UserId == userId && wv.VotingSessionId == votingSessionId)
            .ToListAsync();
        if (existing.Any()) _ctx.WeightedVotes.RemoveRange(existing);
    }

    public async Task AddRangeAsync(IEnumerable<WeightedVote> votes)
        => await _ctx.WeightedVotes.AddRangeAsync(votes);

    public async Task SaveChangesAsync()
        => await _ctx.SaveChangesAsync();

    public async Task<List<WeightedVote>> GetByProjectAsync(string projectId)
        => await _ctx.WeightedVotes
            .Include(wv => wv.CriterionScores)
            .Where(wv => wv.ProjectId == projectId)
            .ToListAsync();
}