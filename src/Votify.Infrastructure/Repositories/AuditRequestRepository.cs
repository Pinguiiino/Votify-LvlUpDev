using Microsoft.EntityFrameworkCore;
using Votify.Domain.AuditFolder;

namespace Votify.Infrastructure.Repositories;

public class AuditRequestRepository : IAuditRequestRepository
{
    private readonly VotifyDbContext _context;

    public AuditRequestRepository(VotifyDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsByProjectIdAsync(string projectId)
        => await _context.AuditRequests.AnyAsync(a => a.ProjectId == projectId);

    public async Task AddAsync(AuditRequest request)
    {
        await _context.AuditRequests.AddAsync(request);
    }

    public async Task<List<string>> GetAllProjectIdsAsync()
        => await _context.AuditRequests
            .Select(a => a.ProjectId)
            .ToListAsync();

    public async Task<List<AuditDashboardItem>> GetDashboardByEventAsync(string eventId)
        => await _context.AuditRequests
            .Join(_context.Projects,
                audit => audit.ProjectId,
                project => project.Id,
                (audit, project) => new { audit, project })
            .Where(x => x.project.EventId == eventId)
            .OrderByDescending(x => x.audit.RequestedAt)
            .Select(x => new AuditDashboardItem(
                x.audit.Id,
                x.project.Id,
                x.project.Title,
                x.audit.RequestedAt))
            .ToListAsync();

    public async Task SaveChangesAsync()
        => await _context.SaveChangesAsync();
}
