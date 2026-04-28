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

    public Task<List<Category>> GetByEventAsync(string eventId)
        => _repository.GetByEventAsync(eventId);

    public Task<Category?> GetWithDetailsAsync(string categoryId)
        => _repository.GetWithDetailsAsync(categoryId);

    public async Task<Category> CreateAsync(CreateCategoryData data)
    {
        var evento = await _eventRepository.GetByIdAsync(data.EventId)
            ?? throw new ArgumentException($"El evento \"{data.EventId}\" no existe.");

        var nombre = (data.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre de la categoría es obligatorio.");

        if (await _repository.ExistsByNameInEventAsync(data.EventId, nombre))
            throw new ArgumentException($"Ya existe una categoría con el nombre \"{nombre}\" en este evento.");

        if (data.VotingSessions == null || data.VotingSessions.Count == 0)
            throw new ArgumentException("La categoría debe tener al menos una votación.");
        if (data.VotingSessions.Count > 2)
            throw new ArgumentException("La categoría puede tener como máximo dos votaciones.");

        var voterTypes = data.VotingSessions.Select(v => v.VoterType).ToList();
        if (voterTypes.Distinct().Count() != voterTypes.Count)
            throw new ArgumentException("Solo puede haber una votación de jurado y/o una de público.");

        var categoria = new Category(
            eventId: data.EventId,
            name: nombre,
            description: string.IsNullOrWhiteSpace(data.Description) ? null : data.Description.Trim(),
            allowSelfVoting: data.AllowSelfVoting,
            combineResults: data.CombineResults,
            juryWeight: data.CombineResults ? data.JuryWeight : null,
            publicWeight: data.CombineResults ? data.PublicWeight : null);

        if (data.CombineResults)
        {
            bool hasJury = voterTypes.Contains(VoterType.Jury);
            bool hasPublic = voterTypes.Contains(VoterType.Public);
            if (!hasJury || !hasPublic)
                throw new ArgumentException("Para combinar resultados deben existir una votación de jurado y una de público.");

            if (!data.JuryWeight.HasValue || !data.PublicWeight.HasValue)
                throw new ArgumentException("Al combinar resultados hay que indicar los pesos de jurado y público.");

            if (data.JuryWeight.Value < 0 || data.PublicWeight.Value < 0)
                throw new ArgumentException("Los pesos no pueden ser negativos.");

            if (Math.Abs((data.JuryWeight.Value + data.PublicWeight.Value) - 1.0) > 0.001)
                throw new ArgumentException("Los pesos de jurado y público deben sumar 1.");
        }

        foreach (var sesData in data.VotingSessions)
        {
            var sesion = BuildSession(categoria, sesData, evento);
            categoria.VotingSessions.Add(sesion);
        }

        BuildPrizes(categoria, data, voterTypes);

        await _repository.AddAsync(categoria);
        await _repository.SaveChangesAsync();
        return categoria;
    }

    private static VotingSession BuildSession(Category categoria, CreateVotingSessionData data, Event evento)
    {
        if (data.EvaluationType == EvaluationType.WeightedScale && data.VoterType != VoterType.Jury)
            throw new ArgumentException("El baremo ponderado solo está permitido para el jurado.");

        if (data.RequireComments && !data.AllowComments)
            throw new ArgumentException("Para exigir comentarios, primero deben estar permitidos.");

        int? topN = null;
        int? pointsPerVoter = null;
        int? maxPointsPerProject = null;

        switch (data.EvaluationType)
        {
            case EvaluationType.TopN:
                if (!data.TopN.HasValue || data.TopN.Value <= 0)
                    throw new ArgumentException("La votación de tipo Top N necesita un valor N mayor que 0.");
                topN = data.TopN.Value;
                break;

            case EvaluationType.PointDistribution:
                if (!data.PointsPerVoter.HasValue || data.PointsPerVoter.Value <= 0)
                    throw new ArgumentException("El reparto de puntos necesita un total de puntos por votante mayor que 0.");
                if (!data.MaxPointsPerProject.HasValue || data.MaxPointsPerProject.Value <= 0)
                    throw new ArgumentException("El reparto de puntos necesita un máximo de puntos por proyecto mayor que 0.");
                if (data.MaxPointsPerProject.Value > data.PointsPerVoter.Value)
                    throw new ArgumentException("El máximo por proyecto no puede superar al total de puntos por votante.");
                pointsPerVoter = data.PointsPerVoter.Value;
                maxPointsPerProject = data.MaxPointsPerProject.Value;
                break;
        }

        var openAt = data.OpenAt ?? evento.StartDate;
        var closeAt = data.CloseAt ?? evento.EndDate;
        if (closeAt <= openAt)
            throw new ArgumentException("La fecha de cierre de una votación debe ser posterior a la de apertura.");

        CriterionType? criterionType = null;
        bool hasCriteria = data.Criteria != null && data.Criteria.Count > 0;

        if (data.EvaluationType == EvaluationType.WeightedScale && !hasCriteria)
            throw new ArgumentException("El baremo ponderado requiere al menos un criterio.");

        if (hasCriteria)
        {
            if (!data.CriterionType.HasValue)
                throw new ArgumentException("Si la votación tiene criterios, hay que indicar el tipo de criterio.");
            criterionType = data.CriterionType.Value;
        }

        var sesion = new VotingSession(
            categoryId: categoria.Id,
            name: (data.Name ?? string.Empty).Trim(),
            voterType: data.VoterType,
            evaluationType: data.EvaluationType,
            openAt: openAt,
            closeAt: closeAt,
            description: string.IsNullOrWhiteSpace(data.Description) ? null : data.Description.Trim(),
            criterionType: criterionType,
            topN: topN,
            pointsPerVoter: pointsPerVoter,
            maxPointsPerProject: maxPointsPerProject,
            allowComments: data.AllowComments,
            requireComments: data.RequireComments,
            allowCommentsPerCriterion: data.AllowCommentsPerCriterion,
            reminderMinutesBeforeClose: data.ReminderMinutesBeforeClose);

        if (string.IsNullOrWhiteSpace(sesion.Name))
            sesion.Name = $"{(data.VoterType == VoterType.Jury ? "Jurado" : "Público")} - {categoria.Name}";

        if (hasCriteria)
        {
            var nombres = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            double totalPeso = 0;
            foreach (var crData in data.Criteria!)
            {
                var crNombre = (crData.Name ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(crNombre))
                    throw new ArgumentException("El nombre de cada criterio es obligatorio.");
                if (!nombres.Add(crNombre))
                    throw new ArgumentException($"Criterios repetidos dentro de la misma votación: \"{crNombre}\".");
                if (crData.Weight < 0)
                    throw new ArgumentException($"El peso del criterio \"{crNombre}\" no puede ser negativo.");
                if (data.EvaluationType == EvaluationType.WeightedScale && crData.Weight <= 0)
                    throw new ArgumentException($"El peso del criterio \"{crNombre}\" es obligatorio y debe ser mayor que 0.");

                sesion.Criteria.Add(new Criterion(
                    votingSessionId: sesion.Id,
                    name: crNombre,
                    weight: crData.Weight,
                    description: string.IsNullOrWhiteSpace(crData.Description) ? null : crData.Description.Trim()));

                totalPeso += crData.Weight;
            }

            if (Math.Abs(totalPeso - 1.0) > 0.001)
                throw new ArgumentException("Los pesos de los criterios deben sumar 1.");
        }

        return sesion;
    }

    private static void BuildPrizes(Category categoria, CreateCategoryData data, List<VoterType> voterTypes)
    {
        if (data.Prizes == null || data.Prizes.Count == 0)
            throw new ArgumentException("La categoría debe tener al menos un premio.");

        if (data.CombineResults)
        {
            var grupo = data.Prizes.ToList();
            ValidatePrizeGroup(grupo, "combinado");
            foreach (var p in grupo)
                categoria.Prizes.Add(new Prize(
                    categoryId: categoria.Id,
                    position: p.Position,
                    name: (p.Name ?? string.Empty).Trim(),
                    description: string.IsNullOrWhiteSpace(p.Description) ? null : p.Description.Trim(),
                    targetVoter: null));
            return;
        }

        foreach (var vt in voterTypes)
        {
            var grupo = data.Prizes.Where(p => p.TargetVoter == vt).ToList();
            if (grupo.Count == 0)
                throw new ArgumentException($"La votación de {(vt == VoterType.Jury ? "jurado" : "público")} necesita al menos un premio.");

            ValidatePrizeGroup(grupo, vt == VoterType.Jury ? "jurado" : "público");
            foreach (var p in grupo)
                categoria.Prizes.Add(new Prize(
                    categoryId: categoria.Id,
                    position: p.Position,
                    name: (p.Name ?? string.Empty).Trim(),
                    description: string.IsNullOrWhiteSpace(p.Description) ? null : p.Description.Trim(),
                    targetVoter: vt));
        }

        var sobrantes = data.Prizes
            .Where(p => p.TargetVoter == null || !voterTypes.Contains(p.TargetVoter.Value))
            .ToList();
        if (sobrantes.Count > 0)
            throw new ArgumentException("Hay premios que no apuntan a ninguna votación existente en la categoría.");
    }

    private static void ValidatePrizeGroup(List<CreatePrizeData> grupo, string etiqueta)
    {
        var posiciones = new HashSet<int>();
        foreach (var p in grupo)
        {
            if (string.IsNullOrWhiteSpace(p.Name))
                throw new ArgumentException($"El nombre de cada premio ({etiqueta}) es obligatorio.");
            if (p.Position <= 0)
                throw new ArgumentException($"La posición de un premio ({etiqueta}) debe ser mayor que 0.");
            if (!posiciones.Add(p.Position))
                throw new ArgumentException($"Hay posiciones repetidas en los premios ({etiqueta}).");
        }
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
    public List<CreatePrizeData> Prizes { get; set; } = new();
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
    public VoterType? TargetVoter { get; set; }
}
