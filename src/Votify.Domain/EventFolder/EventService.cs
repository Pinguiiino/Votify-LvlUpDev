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
        DateTime startDate, DateTime endDate, int topNProjectsAllowed,
        string? description,
        List<CreateCategoryData> categoriasData)
    {
        var creator = new ModalityEventCreator(modality);
        var evento = creator.Create(name, maxProjects, startDate, endDate, topNProjectsAllowed, description);

        var categorias = new List<Category>();
        foreach (var catData in categoriasData)
        {
            var categoria = new Category(
                eventId: evento.Id,
                name: catData.Name,
                description: catData.Description,
                allowSelfVoting: catData.AllowSelfVoting
            );

            foreach (var crData in catData.Criteria)
            {
                var tipo = Enum.TryParse<CriterionType>(crData.Type, out var ct) ? ct : CriterionType.Numeric;
                categoria.Criteria.Add(new Criterion(categoria.Id, crData.Name, tipo, crData.Weight, crData.Description));
            }

            foreach (var prData in catData.Prizes)
            {
                categoria.Prizes.Add(new Prize(categoria.Id, prData.Position, prData.Name, prData.Description));
            }

            categorias.Add(categoria);
        }

        await _repository.AddAsync(evento, categorias);
        await _repository.SaveChangesAsync();
        return evento;
    }
}

public class CreateCategoryData
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool AllowSelfVoting { get; set; }
    public List<CreateCriterionData> Criteria { get; set; } = new();
    public List<CreatePrizeData> Prizes { get; set; } = new();
}

public class CreateCriterionData
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = "Numeric";
    public double Weight { get; set; }
}

public class CreatePrizeData
{
    public int Position { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}