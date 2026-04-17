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
            .Where(c => c.EventId == eventId)
            .AsNoTracking()
            .ToListAsync();

    public async Task<Category?> GetByIdAsync(string categoryId)
        => await _context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == categoryId);
}
