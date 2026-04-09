namespace Votify.Domain.CategoryFolder;

public interface ICategoryRepository
{
    Task<List<Category>> GetByEventAsync(string eventId);
}