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
        => await _context.Events.AsNoTracking().ToListAsync();

    public async Task<Event?> GetByIdAsync(string id)
        => await _context.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);

    public async Task<List<Category>> GetCategoriesWithDetailsAsync(string eventId)
        => await _context.Categories
            .Include(c => c.Criteria)
            .Include(c => c.Prizes)
            .Where(c => c.EventId == eventId)
            .AsNoTracking()
            .ToListAsync();

    public async Task AddAsync(Event evento, List<Category> categorias)
    {
        await _context.Events.AddAsync(evento);
        await _context.Categories.AddRangeAsync(categorias);
    }

    public async Task SaveChangesAsync()
        => await _context.SaveChangesAsync();
}