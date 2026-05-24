using Microsoft.EntityFrameworkCore;
using Votify.Domain.ProjectFolder;

namespace Votify.Infrastructure.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly VotifyDbContext _context;

    public ProjectRepository(VotifyDbContext context)
    {
        _context = context;
    }

    public async Task<Project?> GetByIdAsync(string id)
        => await _context.Projects
            .Include(p => p.Materials)
            .Include(p => p.ProjectCategories)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<List<Project>> GetAllAsync()
        => await _context.Projects
            .Include(p => p.Materials)
            .Include(p => p.ProjectCategories)
            .AsNoTracking()
            .ToListAsync();

    public async Task<List<Project>> GetByCategoryAsync(string categoryId)
        => await _context.Projects
            .Include(p => p.Materials)
            .Include(p => p.ProjectCategories)
            .Where(p => p.ProjectCategories.Any(pc => pc.CategoryId == categoryId)
                        && p.ValidationStatus == ValidationStatus.Approved)
            .AsNoTracking()
            .ToListAsync();

    public async Task<List<Project>> GetByEventAsync(string eventId)
        => await _context.Projects
            .AsNoTracking()
            .Where(p => p.EventId == eventId)
            .ToListAsync();

    public async Task<List<Project>> GetPendingByEventAsync(string eventId)
        => await _context.Projects
            .Include(p => p.ProjectCategories)
                .ThenInclude(pc => pc.Category)
            .AsNoTracking()
            .Where(p => p.EventId == eventId
                        && p.ValidationStatus == ValidationStatus.Pending)
            .ToListAsync();

    public async Task AddAsync(Project project)
        => await _context.Projects.AddAsync(project);

    public async Task SaveChangesAsync()
        => await _context.SaveChangesAsync();

    public async Task DeleteAsync(string projectId)
    {
        // Votes reference VotedProjectId but have no FK cascade from Project — delete manually
        var votes = await _context.Votes
            .Where(v => v.VotedProjectId == projectId)
            .ToListAsync();
        _context.Votes.RemoveRange(votes);

        // WeightedVotes reference ProjectId but have no FK cascade — delete manually
        var weightedVotes = await _context.WeightedVotes
            .Where(wv => wv.ProjectId == projectId)
            .ToListAsync();
        _context.WeightedVotes.RemoveRange(weightedVotes);

        // AuditRequests have no FK cascade — delete manually
        var auditRequests = await _context.AuditRequests
            .Where(ar => ar.ProjectId == projectId)
            .ToListAsync();
        _context.AuditRequests.RemoveRange(auditRequests);

        // Delete the project — ProjectCategories and Materials cascade from Project
        var project = await _context.Projects.FindAsync(projectId);
        if (project != null)
            _context.Projects.Remove(project);
    }

    public async Task<bool> TitleExistsInEventAsync(string title, string eventId)
        => await _context.Projects
            .AnyAsync(p => p.Title == title && p.EventId == eventId);

    public async Task<List<Project>> GetByOwnerAsync(string ownerId)
    => await _context.Projects
        .Include(p => p.Materials)
        .Include(p => p.ProjectCategories)
        .AsNoTracking()
        .Where(p => p.OwnerId == ownerId)
        .ToListAsync();
}
