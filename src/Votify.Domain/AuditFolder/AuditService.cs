using Votify.Domain.VoteFolder;

namespace Votify.Domain.AuditFolder;

public class AuditService
{
    private readonly IAuditRequestRepository _auditRepository;
    private readonly IVoteRepository _voteRepository;

    public AuditService(IAuditRequestRepository auditRepository, IVoteRepository voteRepository)
    {
        _auditRepository = auditRepository;
        _voteRepository = voteRepository;
    }

    public async Task<List<AuditTrailEntry>> GetProjectAuditAsync(string projectId)
    {
        var votes = await _voteRepository.GetByProjectAsync(projectId);
        return votes
            .Select(v => new AuditTrailEntry(
                v.UserId,
                v.CreatedAt,
                v.IntegrityHash,
                v.TopPosition,
                v.Comment))
            .ToList();
    }

    public async Task RequestAuditAsync(string projectId)
    {
        bool exists = await _auditRepository.ExistsByProjectIdAsync(projectId);
        if (exists) return;

        await _auditRepository.AddAsync(new AuditRequest(projectId));
        await _auditRepository.SaveChangesAsync();
    }

    public Task<List<string>> GetRequestedProjectIdsAsync()
        => _auditRepository.GetAllProjectIdsAsync();

    public Task<List<AuditDashboardItem>> GetDashboardByEventAsync(string eventId)
        => _auditRepository.GetDashboardByEventAsync(eventId);
}

public record AuditTrailEntry(string Voter, DateTime Date, string? Hash, int TopPosition, string? Comment);
