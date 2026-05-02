namespace Votify.Domain.ProjectFolder;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(string id);
    Task<List<Project>> GetAllAsync();
    Task<List<Project>> GetByCategoryAsync(string categoryId);
    Task<List<Project>> GetByEventAsync(string eventId);
    Task<List<Project>> GetByOwnerAsync(string ownerId);
    Task AddAsync(Project project);
    Task SaveChangesAsync();
    Task<bool> TitleExistsInEventAsync(string title, string eventId);
}
