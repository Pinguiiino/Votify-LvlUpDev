namespace Votify.Domain.ProjectFolder;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(string id);
    Task<List<Project>> GetAllAsync();
    Task<List<Project>> GetByCategoryAsync(string categoryId);
    Task AddAsync(Project project);
    Task SaveChangesAsync();
}