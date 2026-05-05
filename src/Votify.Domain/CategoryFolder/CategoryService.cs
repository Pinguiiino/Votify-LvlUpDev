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

        ValidateEvaluationParameters(data);

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

        if (data.VoterType == VoterType.Jury && data.JurorEmails != null)
        {
            sesion.JurorEmails = data.JurorEmails.Select(e => e.Trim().ToLower()).Distinct().ToList();
        }

        if (data.Criteria != null)
        {
            foreach (var cr in data.Criteria)
            {
                sesion.Criteria.Add(new Criterion(sesion.Id, cr.Name, cr.Weight, cr.Description));
            }
        }

        if (data.Prizes == null || data.Prizes.Count == 0)
            throw new ArgumentException($"La votación '{sesion.Name}' debe tener al menos un premio.");

        if (data.Prizes.Count > evento.MaxProjects)
            throw new ArgumentException(
                $"La votación '{sesion.Name}' no puede tener más premios ({data.Prizes.Count}) que el número máximo de proyectos del evento ({evento.MaxProjects}).");

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

    private static void ValidateEvaluationParameters(CreateVotingSessionData data)
    {
        switch (data.EvaluationType)
        {
            case EvaluationType.TopN:
                if (!data.TopN.HasValue || data.TopN.Value <= 0)
                    throw new ArgumentException("En una votación Top N el número de proyectos a votar debe ser mayor que 0.");
                break;

            case EvaluationType.PointDistribution:
                if (!data.PointsPerVoter.HasValue || data.PointsPerVoter.Value <= 0)
                    throw new ArgumentException("En una votación por reparto de puntos, el total de puntos por votante debe ser mayor que 0.");
                if (data.MaxPointsPerProject.HasValue && data.MaxPointsPerProject.Value <= 0)
                    throw new ArgumentException("El máximo de puntos por proyecto debe ser mayor que 0.");
                if (data.MaxPointsPerProject.HasValue && data.MaxPointsPerProject.Value > data.PointsPerVoter.Value)
                    throw new ArgumentException("El máximo de puntos por proyecto no puede superar el total de puntos por votante.");
                break;

            case EvaluationType.WeightedScale:
                if (data.Criteria == null || data.Criteria.Count == 0)
                    throw new ArgumentException("En una votación con baremo debe haber al menos un criterio.");
                if (data.Criteria.Any(c => c.Weight <= 0))
                    throw new ArgumentException("Todos los criterios deben tener un peso mayor que 0.");
                break;
        }
    }

    public async Task UpdateVotingTypeAsync(string categoryId, UpdateCategoryVotingData data)
    {
        var categoria = await _repository.GetForUpdateAsync(categoryId)
            ?? throw new ArgumentException("Categoría no existe.");

        var evento = await _eventRepository.GetByIdAsync(categoria.EventId)
            ?? throw new ArgumentException("Evento no existe.");

        if (data.VotingSessions == null || data.VotingSessions.Count == 0)
            throw new ArgumentException("Debe haber al menos una votación.");

        var now = DateTime.UtcNow;
        foreach (var existente in categoria.VotingSessions)
        {
            if (existente.OpenAt <= now)
                throw new InvalidOperationException(
                    $"No se puede modificar la votación '{existente.Name}' porque su ventana ya ha comenzado.");
        }

        foreach (var sesData in data.VotingSessions)
            ValidateEvaluationParameters(sesData);

        foreach (var sesData in data.VotingSessions)
        {
            if (sesData.Prizes == null || sesData.Prizes.Count == 0)
                throw new ArgumentException("Cada votación debe tener al menos un premio.");
            if (sesData.Prizes.Count > evento.MaxProjects)
                throw new ArgumentException(
                    $"Una votación no puede tener más premios ({sesData.Prizes.Count}) que el número máximo de proyectos del evento ({evento.MaxProjects}).");
        }

        categoria.CombineResults = data.CombineResults;
        categoria.JuryWeight = data.CombineResults ? data.JuryWeight : null;
        categoria.PublicWeight = data.CombineResults ? data.PublicWeight : null;
        categoria.AllowSelfVoting = data.AllowSelfVoting;

        await _repository.RemoveVotingSessionsAsync(categoria);

        foreach (var sesData in data.VotingSessions)
        {
            var sesion = BuildSession(categoria, sesData, evento);
            categoria.VotingSessions.Add(sesion);
        }

        await _repository.SaveChangesAsync();
    }
}

public class UpdateCategoryVotingData
{
    public bool AllowSelfVoting { get; set; }
    public bool CombineResults { get; set; }
    public double? JuryWeight { get; set; }
    public double? PublicWeight { get; set; }
    public List<CreateVotingSessionData> VotingSessions { get; set; } = new();
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