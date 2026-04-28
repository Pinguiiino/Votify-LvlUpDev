namespace Votify.Domain.AuditFolder;

public interface IAuditRequestRepository
{
    Task<bool> ExistsByProjectIdAsync(string projectId);
    Task AddAsync(AuditRequest request);
    Task<List<string>> GetAllProjectIdsAsync();
    Task<List<AuditDashboardItem>> GetDashboardByEventAsync(string eventId);
    Task SaveChangesAsync();
}

public record AuditDashboardItem(string AuditId, string ProjectId, string ProjectTitle, DateTime RequestedAt);
