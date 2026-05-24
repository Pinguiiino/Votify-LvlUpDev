namespace Votify.Domain.CategoryFolder;

public interface ICategoryRepository
{
    Task<List<Category>> GetByEventAsync(string eventId);
    Task<Category?> GetByIdAsync(string categoryId);
    Task<Category?> GetWithDetailsAsync(string categoryId);
    Task<Category?> GetForUpdateAsync(string categoryId);
    Task AddAsync(Category category);
    Task RemoveVotingSessionsAsync(Category category);
    Task<bool> ExistsByNameInEventAsync(string eventId, string name);
    Task DeleteAsync(string categoryId);
    Task SaveChangesAsync();
}
