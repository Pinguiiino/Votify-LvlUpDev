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
            .Include(c => c.VotingSessions)
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
            .Include(c => c.VotingSessions)
                .ThenInclude(vs => vs.Prizes)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == categoryId);

    public async Task<Category?> GetForUpdateAsync(string categoryId)
        => await _context.Categories
            .Include(c => c.VotingSessions)
                .ThenInclude(vs => vs.Criteria)
            .Include(c => c.VotingSessions)
                .ThenInclude(vs => vs.Prizes)
            .FirstOrDefaultAsync(c => c.Id == categoryId);

    public async Task AddAsync(Category category)
    {
        await _context.Categories.AddAsync(category);
    }

    public Task RemoveVotingSessionsAsync(Category category)
    {
        foreach (var sesion in category.VotingSessions.ToList())
        {
            _context.RemoveRange(sesion.Criteria);
            _context.RemoveRange(sesion.Prizes);
        }
        _context.RemoveRange(category.VotingSessions);
        category.VotingSessions.Clear();
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsByNameInEventAsync(string eventId, string name)
    {
        var nombreLimpio = name.Trim().ToLower();
        return await _context.Categories
            .AnyAsync(c => c.EventId == eventId && c.Name.ToLower() == nombreLimpio);
    }

    public async Task DeleteAsync(string categoryId)
    {
        // Get VotingSession IDs for this category
        var sessionIds = await _context.VotingSessions
            .Where(vs => vs.CategoryId == categoryId)
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

        // ProjectCategory → Category is Restrict — delete these records manually first
        var projectCategories = await _context.ProjectCategories
            .Where(pc => pc.CategoryId == categoryId)
            .ToListAsync();
        _context.ProjectCategories.RemoveRange(projectCategories);

        // Delete the category — VotingSessions → Criteria/Prizes/Votes cascade
        var categoria = await _context.Categories.FindAsync(categoryId);
        if (categoria != null)
            _context.Categories.Remove(categoria);
    }

    public async Task SaveChangesAsync()
        => await _context.SaveChangesAsync();
}