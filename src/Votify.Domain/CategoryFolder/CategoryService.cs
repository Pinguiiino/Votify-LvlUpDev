namespace Votify.Domain.CategoryFolder;

public class CategoryService
{
    private readonly ICategoryRepository _repository;

    public CategoryService(ICategoryRepository repository)
    {
        _repository = repository;
    }

    public Task<List<Category>> GetByEventAsync(string eventId)
        => _repository.GetByEventAsync(eventId);
}