using Votify.Domain.EventFolder;
using Votify.Domain.VoteFolder;

namespace Votify.Domain.CategoryFolder;

public class CategoryService
{
    private readonly ICategoryRepository _repository;
    private readonly IEventRepository _eventRepository;

    public CategoryService(ICategoryRepository repository, IEventRepository eventRepository)
    {
        _repository = repository;
        _eventRepository = eventRepository;
    }

    public Task<List<Category>> GetByEventAsync(string eventId) => _repository.GetByEventAsync(eventId);
    public Task<Category?> GetWithDetailsAsync(string categoryId) => _repository.GetWithDetailsAsync(categoryId);

    public async Task<Category> CreateAsync(CreateCategoryData data)
    {
        var evento = await _eventRepository.GetByIdAsync(data.EventId) ?? throw new ArgumentException("Evento no existe.");
        var nombre = (data.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nombre)) throw new ArgumentException("Nombre obligatorio.");
        if (await _repository.ExistsByNameInEventAsync(data.EventId, nombre)) throw new ArgumentException("Categoría duplicada.");
        if (data.VotingSessions == null || data.VotingSessions.Count == 0) throw new ArgumentException("Debe haber al menos una votación.");

        var categoria = new Category(
            eventId: data.EventId,
            name: nombre,
            description: string.IsNullOrWhiteSpace(data.Description) ? null : data.Description.Trim(),
            allowSelfVoting: data.AllowSelfVoting,
            combineResults: data.CombineResults,
            juryWeight: data.CombineResults ? data.JuryWeight : null,
            publicWeight: data.CombineResults ? data.PublicWeight : null);

        foreach (var sesData in data.VotingSessions)
        {
            var sesion = BuildSession(categoria, sesData, evento);
            categoria.VotingSessions.Add(sesion);
        }

        await _repository.AddAsync(categoria);
        await _repository.SaveChangesAsync();
        return categoria;
    }

    private static VotingSession BuildSession(Category categoria, CreateVotingSessionData data, Event evento)
    {
        var openAt = data.OpenAt ?? evento.StartDate;
        var closeAt = data.CloseAt ?? evento.EndDate;

        var sesion = new VotingSession(
            categoryId: categoria.Id,
            name: data.Name ?? $"{(data.VoterType == VoterType.Jury ? "Jurado" : "Público")} - {categoria.Name}",
            voterType: data.VoterType,
            evaluationType: data.EvaluationType,
            openAt: openAt,
            closeAt: closeAt,
            description: data.Description,
            criterionType: data.CriterionType,
            topN: data.TopN,
            pointsPerVoter: data.PointsPerVoter,
            maxPointsPerProject: data.MaxPointsPerProject,
            allowComments: data.AllowComments,
            requireComments: data.RequireComments,
            allowCommentsPerCriterion: data.AllowCommentsPerCriterion);

        // 1. Asignar Jurados
        if (data.VoterType == VoterType.Jury && data.JurorEmails != null)
        {
            sesion.JurorEmails = data.JurorEmails.Select(e => e.Trim().ToLower()).Distinct().ToList();
        }

        // 2. Asignar Criterios
        if (data.Criteria != null)
        {
            foreach (var cr in data.Criteria)
            {
                sesion.Criteria.Add(new Criterion(sesion.Id, cr.Name, cr.Weight, cr.Description));
            }
        }

        // 3. Asignar Premios (¡Movido aquí!)
        if (data.Prizes == null || data.Prizes.Count == 0)
            throw new ArgumentException($"La votación '{sesion.Name}' debe tener al menos un premio.");

        var posiciones = new HashSet<int>();
        foreach (var p in data.Prizes)
        {
            if (string.IsNullOrWhiteSpace(p.Name)) throw new ArgumentException("Nombre de premio obligatorio.");
            if (p.Position <= 0) throw new ArgumentException("La posición debe ser mayor que 0.");
            if (!posiciones.Add(p.Position)) throw new ArgumentException("Posiciones de premios repetidas en la votación.");

            sesion.Prizes.Add(new Prize(sesion.Id, p.Position, p.Name, p.Description));
        }

        return sesion;
    }
}

public class CreateCategoryData
{
    public string EventId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool AllowSelfVoting { get; set; }
    public bool CombineResults { get; set; }
    public double? JuryWeight { get; set; }
    public double? PublicWeight { get; set; }
    public List<CreateVotingSessionData> VotingSessions { get; set; } = new();
}

public class CreateVotingSessionData
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public VoterType VoterType { get; set; }
    public EvaluationType EvaluationType { get; set; }
    public CriterionType? CriterionType { get; set; }
    public int? TopN { get; set; }
    public int? PointsPerVoter { get; set; }
    public int? MaxPointsPerProject { get; set; }
    public bool AllowComments { get; set; }
    public bool RequireComments { get; set; }
    public bool AllowCommentsPerCriterion { get; set; }
    public DateTime? OpenAt { get; set; }
    public DateTime? CloseAt { get; set; }
    public int? ReminderMinutesBeforeClose { get; set; }
    public List<CreateCriterionData> Criteria { get; set; } = new();
    public List<CreatePrizeData> Prizes { get; set; } = new();
    public List<string> JurorEmails { get; set; } = new();
}

public class CreateCriterionData
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public double Weight { get; set; }
}

public class CreatePrizeData
{
    public int Position { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}