using Votify.Domain.CategoryFolder;
using Votify.Domain.Factory;
using Votify.Domain.VoteFolder;

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
        string? imageUrl,
        List<CreateCategoryData> categoriasData)
    {
        bool nombreOcupado = await _repository.ExistsByNameAsync(name);

        if (nombreOcupado)
        {
            throw new ArgumentException($"Ya existe un evento con el nombre \"{name}\". Elige un nombre diferente.");
        }

        var creator = new ModalityEventCreator(modality);
        var evento = creator.Create(name, maxProjects, startDate, endDate, description, imageUrl);

        var votingSession = new VotingSession(
            eventId: evento.Id,
            name: $"Votación principal - {evento.Name}",
            openAt: startDate,
            closeAt: endDate,
            description: "Sesión de votación general del evento.",
            reminderMinutesBeforeClose: 60
        );
        evento.VotingSessions.Add(votingSession);

        var categorias = new List<Category>();

        var nombresProcesados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var catData in categoriasData)
        {
            var nombreLimpio = catData.Name.Trim();

            if (!nombresProcesados.Add(nombreLimpio))
            {
                throw new ArgumentException($"El evento no puede tener dos categorías con el mismo nombre: \"{nombreLimpio}\".");
            }

            var topN = catData.TopNProjectsAllowed > 0 ? catData.TopNProjectsAllowed : 3;

            var categoria = new Category(
                eventId: evento.Id,
                name: nombreLimpio,
                description: catData.Description,
                allowSelfVoting: catData.AllowSelfVoting,
                topNProjectsAllowed: topN
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
    public int TopNProjectsAllowed { get; set; } = 3;
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
