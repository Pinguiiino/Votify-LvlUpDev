using Microsoft.EntityFrameworkCore;
using Votify.Domain.CategoryFolder;
using Votify.Domain.EventFolder;

namespace Votify.Infrastructure.Repositories;

public class EventRepository : IEventRepository
{
    private readonly VotifyDbContext _context;

    public EventRepository(VotifyDbContext context)
    {
        _context = context;
    }

    public async Task<List<Event>> GetAllAsync()
    {
        return await _context.Events
            .Include(e => e.Participants)
            .Include(e => e.Public)
            .ToListAsync();
    }

    public async Task<Event?> GetByIdAsync(string id)
    {
        return await _context.Events
            .Include(e => e.Participants)
            .Include(e => e.Public)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<Category>> GetCategoriesWithDetailsAsync(string eventId)
        => await _context.Categories
            .Include(c => c.VotingSessions)
                .ThenInclude(vs => vs.Criteria)
            .Include(c => c.VotingSessions) // ── AHORA CARGA LOS PREMIOS DESDE LA SESIÓN ──
                .ThenInclude(vs => vs.Prizes)
            .Where(c => c.EventId == eventId)
            .AsNoTracking()
            .ToListAsync();

    public async Task AddAsync(Event evento)
    {
        await _context.Events.AddAsync(evento);
    }

    public async Task<bool> ExistsByNameAsync(string name)
    {
        var nombreLimpio = name.Trim().ToLower();
        return await _context.Events.AnyAsync(e => e.Name.ToLower() == nombreLimpio);
    }

    public async Task DeleteAsync(string eventId)
    {
        // Collect all VotingSession IDs belonging to this event's categories
        var sessionIds = await _context.VotingSessions
            .Where(vs => _context.Categories.Any(c => c.EventId == eventId && c.Id == vs.CategoryId))
            .Select(vs => vs.Id)
            .ToListAsync();

        // WeightedVotes have no FK cascade from VotingSession — delete manually
        if (sessionIds.Any())
        {
            var weightedVotes = await _context.WeightedVotes
                .Where(wv => sessionIds.Contains(wv.VotingSessionId))
                .ToListAsync();
            _context.WeightedVotes.RemoveRange(weightedVotes);
        }

        // Collect all project IDs for this event
        var projectIds = await _context.Projects
            .Where(p => p.EventId == eventId)
            .Select(p => p.Id)
            .ToListAsync();

        if (projectIds.Any())
        {
            // AuditRequests have no FK cascade — delete manually
            var auditRequests = await _context.AuditRequests
                .Where(ar => projectIds.Contains(ar.ProjectId))
                .ToListAsync();
            _context.AuditRequests.RemoveRange(auditRequests);

            // Votes by VotedProjectId have no FK cascade from Project — delete manually
            var votes = await _context.Votes
                .Where(v => projectIds.Contains(v.VotedProjectId))
                .ToListAsync();
            _context.Votes.RemoveRange(votes);

            // WeightedVotes by ProjectId — delete manually
            var weightedByProject = await _context.WeightedVotes
                .Where(wv => projectIds.Contains(wv.ProjectId))
                .ToListAsync();
            _context.WeightedVotes.RemoveRange(weightedByProject);

            // Delete projects (ProjectCategories and Materials cascade from Project)
            var projects = await _context.Projects
                .Where(p => p.EventId == eventId)
                .ToListAsync();
            _context.Projects.RemoveRange(projects);
        }

        // Delete the event — Categories → VotingSessions → Criteria/Prizes/Votes cascade
        var evento = await _context.Events.FindAsync(eventId);
        if (evento != null)
            _context.Events.Remove(evento);
    }

    public async Task SaveChangesAsync()
        => await _context.SaveChangesAsync();
}
