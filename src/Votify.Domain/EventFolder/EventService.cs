using Votify.Domain.CategoryFolder;
using Votify.Domain.Factory;

namespace Votify.Domain.EventFolder;

public class EventService
{
    private readonly IEventRepository _repository;

    public EventService(IEventRepository repository)
    {
        _repository = repository;
    }

    public Task<List<Event>> GetAllAsync() => _repository.GetAllAsync();

    public Task<Event?> GetByIdAsync(string id) => _repository.GetByIdAsync(id);

    public Task<List<Category>> GetCategoriesWithDetailsAsync(string eventId)
        => _repository.GetCategoriesWithDetailsAsync(eventId);

    public async Task<Event> CreateEventAsync(
        string name, string modality, int maxProjects,
        DateTime startDate, DateTime endDate,
        string? description,
        string? imageUrl)
    {
        bool nombreOcupado = await _repository.ExistsByNameAsync(name);
        if (nombreOcupado)
            throw new ArgumentException($"Ya existe un evento con el nombre \"{name}\". Elige un nombre diferente.");

        var creator = new ModalityEventCreator(modality);
        var evento = creator.Create(name, maxProjects, startDate, endDate, description, imageUrl);

        await _repository.AddAsync(evento);
        await _repository.SaveChangesAsync();
        return evento;
    }
}
