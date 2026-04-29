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
            .Include(c => c.Prizes)
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

    public async Task SaveChangesAsync()
        => await _context.SaveChangesAsync();
}
