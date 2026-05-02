using Microsoft.EntityFrameworkCore;
using Votify.Domain.CategoryFolder;

namespace Votify.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly VotifyDbContext _context;

    public CategoryRepository(VotifyDbContext context)
    {
        _context = context;
    }

    public async Task<List<Category>> GetByEventAsync(string eventId)
        => await _context.Categories
            .Include(c => c.VotingSessions)
                .ThenInclude(vs => vs.Criteria)
            .Include(c => c.VotingSessions) // ── AHORA CARGA LOS PREMIOS DESDE LA SESIÓN ──
                .ThenInclude(vs => vs.Prizes)
            .Where(c => c.EventId == eventId)
            .AsNoTracking()
            .ToListAsync();

    public async Task<Category?> GetByIdAsync(string categoryId)
        => await _context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == categoryId);

    public async Task<Category?> GetWithDetailsAsync(string categoryId)
        => await _context.Categories
            .Include(c => c.VotingSessions)
                .ThenInclude(vs => vs.Criteria)
            .Include(c => c.VotingSessions) // ── AHORA CARGA LOS PREMIOS DESDE LA SESIÓN ──
                .ThenInclude(vs => vs.Prizes)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == categoryId);

    public async Task AddAsync(Category category)
    {
        await _context.Categories.AddAsync(category);
    }

    public async Task<bool> ExistsByNameInEventAsync(string eventId, string name)
    {
        var nombreLimpio = name.Trim().ToLower();
        return await _context.Categories
            .AnyAsync(c => c.EventId == eventId && c.Name.ToLower() == nombreLimpio);
    }

    public async Task SaveChangesAsync()
        => await _context.SaveChangesAsync();
}