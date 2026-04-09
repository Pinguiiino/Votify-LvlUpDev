using Votify.Domain.CategoryFolder;

namespace Votify.Domain.EventFolder;

public interface IEventRepository
{
    Task<List<Event>> GetAllAsync();
    Task<Event?> GetByIdAsync(string id);
    Task<List<Category>> GetCategoriesWithDetailsAsync(string eventId);
    Task AddAsync(Event evento, List<Category> categorias);
    Task SaveChangesAsync();
}