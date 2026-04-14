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
            .Where(p => p.ProjectCategories.Any(pc => pc.CategoryId == categoryId))
            .AsNoTracking()
            .ToListAsync();

    public async Task AddAsync(Project project)
        => await _context.Projects.AddAsync(project);

    public async Task SaveChangesAsync()
        => await _context.SaveChangesAsync();

    public async Task<bool> TitleExistsInCategoriesAsync(string title, List<string> categoryIds)
    => await _context.Projects
        .AnyAsync(p =>
            p.Title == title &&
            p.ProjectCategories.Any(pc => categoryIds.Contains(pc.CategoryId!)));
}