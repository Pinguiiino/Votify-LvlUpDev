using Votify.Domain.CategoryFolder;
using Votify.Domain.EventFolder;
using Votify.Domain.Factory;
using Votify.Domain.VoteFolder;

namespace Votify.Domain.ProjectFolder;

public class ProjectService
{
    private readonly IProjectRepository _repository;
    private readonly IEventRepository _eventRepository;
    private readonly ICategoryRepository _categoryRepository;

    public ProjectService(IProjectRepository repository, IEventRepository eventRepository, ICategoryRepository categoryRepository)
    {
        _repository = repository;
        _eventRepository = eventRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<Project> CreateProjectAsync(
        string title, string eventId, string? description,
        string projectType, string? imageUrl,
        string? ownerId,
        List<string> categoryIds,
        List<(MaterialType type, string url, string? desc)> materials)
    {
        var evento = await _eventRepository.GetByIdAsync(eventId)
        ?? throw new ArgumentException("Evento no encontrado.");

        var categoriasDelEvento = await _categoryRepository.GetByEventAsync(eventId);
        var categoriasBlockeadas = categoriasDelEvento
            .Where(c => categoryIds.Contains(c.Id) && c.VotingSessions.Any(vs =>
                vs.ManualStatus == "open" ||
                vs.ManualStatus == "paused" ||
                vs.ManualStatus == "closed" ||
                (vs.OpenAt.HasValue && vs.OpenAt.Value <= DateTime.UtcNow)))
            .Select(c => c.Name)
            .ToList();

        if (categoriasBlockeadas.Any())
            throw new ArgumentException(
                $"La votación ya ha iniciado en: {string.Join(", ", categoriasBlockeadas)}. No se pueden subir proyectos a estas categorías.");

        var now = DateTime.UtcNow;

        if (now < evento.StartDate)
            throw new ArgumentException("El evento aún no ha comenzado. No se pueden subir proyectos.");

        if (now > evento.EndDate)
            throw new ArgumentException("El evento ha finalizado. No se pueden subir proyectos.");

        ProjectCreator creator = projectType switch
        {
            "AI" => new AiProjectCreator(),
            "Sustainability" => new SustainabilityProjectCreator(),
            "General" => new GeneralProjectCreator(),
            _ => throw new ArgumentException($"Tipo desconocido: {projectType}")
        };

        bool titleTaken = await _repository.TitleExistsInEventAsync(title, eventId);
        if (titleTaken)
            throw new ArgumentException(
                $"Ya existe un proyecto con el título \"{title}\" en este evento. Elige otro nombre.");

        var project = creator.Create(title, eventId, ownerId, description, imageUrl);
        project.ValidationStatus = ValidationStatus.Pending;

        foreach (var (type, url, desc) in materials)
            project.Materials.Add(new ProjectMaterial(project.Id, type, url, desc));

        foreach (var categoryId in categoryIds)
            project.ProjectCategories.Add(new ProjectCategory(project.Id, categoryId));

        await _repository.AddAsync(project);
        await _repository.SaveChangesAsync();
        return project;
    }

    public async Task<Project> UpdateProjectAsync(
        string projectId, string requesterId,
        string? description, string? imageUrl,
        List<(MaterialType type, string url, string? desc)> materials)
    {
        var project = await _repository.GetByIdAsync(projectId)
            ?? throw new ArgumentException("Proyecto no encontrado.");

        if (!string.IsNullOrEmpty(project.OwnerId)
            && !string.Equals(project.OwnerId, requesterId, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("No tienes permiso para editar este proyecto.");

        var evento = await _eventRepository.GetByIdAsync(project.EventId);
        if (evento != null && DateTime.UtcNow > evento.EndDate)
            throw new InvalidOperationException(
                "El evento ha finalizado y el proyecto ya no se puede modificar.");

        project.Description = description;
        project.ImageUrl = imageUrl;

        project.Materials.Clear();
        foreach (var (type, url, desc) in materials)
            project.Materials.Add(new ProjectMaterial(project.Id, type, url, desc));

        await _repository.SaveChangesAsync();
        return project;
    }

    public Task<Project?> GetByIdAsync(string id) => _repository.GetByIdAsync(id);

    public Task<List<Project>> GetAllAsync() => _repository.GetAllAsync();

    public Task<List<Project>> GetByCategoryAsync(string categoryId)
        => _repository.GetByCategoryAsync(categoryId);

    public Task<List<Project>> GetByOwnerAsync(string ownerId)
    => _repository.GetByOwnerAsync(ownerId);

    public Task<List<Project>> GetPendingByEventAsync(string eventId)
        => _repository.GetPendingByEventAsync(eventId);

    private async Task<(Project project, Event evento)> GetValidatedProjectForApprovalAsync(
        string projectId, string requesterId)
    {
        var project = await _repository.GetByIdAsync(projectId)
            ?? throw new ArgumentException("Proyecto no encontrado.");

        var evento = await _eventRepository.GetByIdAsync(project.EventId)
            ?? throw new ArgumentException("Evento no encontrado.");

        if (!string.Equals(evento.Organizer, requesterId, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("Solo el organizador del evento puede validar proyectos.");

        if (project.ValidationStatus != ValidationStatus.Pending)
            throw new InvalidOperationException("Este proyecto ya ha sido validado previamente.");

        return (project, evento);
    }

    public async Task ApproveAsync(string projectId, string requesterId)
    {
        var (project, _) = await GetValidatedProjectForApprovalAsync(projectId, requesterId);
        project.ValidationStatus = ValidationStatus.Approved;
        project.RejectionReason = null;
        await _repository.SaveChangesAsync();
    }

    public async Task RejectAsync(string projectId, string requesterId, string? reason)
    {
        var (project, _) = await GetValidatedProjectForApprovalAsync(projectId, requesterId);
        project.ValidationStatus = ValidationStatus.Rejected;
        project.RejectionReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        await _repository.SaveChangesAsync();
    }

    public async Task DeleteAsync(string projectId, string requesterId)
    {
        var project = await _repository.GetByIdAsync(projectId)
            ?? throw new ArgumentException("Proyecto no encontrado.");

        if (!string.IsNullOrEmpty(project.OwnerId) &&
            !string.Equals(project.OwnerId, requesterId, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("Solo el creador del proyecto puede eliminarlo.");

        await _repository.DeleteAsync(projectId);
        await _repository.SaveChangesAsync();
    }

    public async Task<ProjectResultsDto?> GetResultsAsync(string projectId, EventService eventService)
    {
        var project = await _repository.GetByIdAsync(projectId);
        if (project == null) return null;

        var evento = await _eventRepository.GetByIdAsync(project.EventId);
        if (evento == null) return null;

        // Reutilizamos el dashboard del evento, que ya calcula puntos
        // para los 3 tipos de evaluación (TopN, PointDistribution, WeightedScale).
        var dashboard = await eventService.GetDashboardStatsAsync(project.EventId);
        var ranking = dashboard?.Ranking ?? new List<ProjectResultDto>();

        var dto = new ProjectResultsDto
        {
            ProjectId = project.Id,
            ProjectTitle = project.Title,
            EventId = evento.Id,
            EventName = evento.Name,
            EventEndDate = evento.EndDate
        };

        // Cargar las categorías del evento con sus sesiones de votación
        var categoriasDelEvento = await eventService.GetCategoriesWithDetailsAsync(evento.Id);

        // Por cada ProjectCategory del proyecto, construimos el resultado
        foreach (var pc in project.ProjectCategories)
        {
            if (pc.CategoryId == null) continue;

            var cat = categoriasDelEvento.FirstOrDefault(c => c.Id == pc.CategoryId);
            if (cat == null) continue;

            var catResult = new CategoryResultDto
            {
                CategoryId = cat.Id,
                CategoryName = cat.Name
            };

            var sesionJurado = cat.VotingSessions.FirstOrDefault(vs => vs.VoterType == VoterType.Jury);
            var sesionPublico = cat.VotingSessions.FirstOrDefault(vs => vs.VoterType == VoterType.Public);

            if (sesionJurado != null)
                catResult.JurySession = BuildSessionResult(sesionJurado, cat.Name, project.Title, ranking);
            if (sesionPublico != null)
                catResult.PublicSession = BuildSessionResult(sesionPublico, cat.Name, project.Title, ranking);

            dto.CategoryResults.Add(catResult);
        }

        // El certificado se habilita si CUALQUIER sesión de CUALQUIER categoría está cerrada
        dto.CanGenerateCertificate = dto.CategoryResults.Any(cr =>
            (cr.JurySession?.IsClosed ?? false) ||
            (cr.PublicSession?.IsClosed ?? false));

        return dto;
    }

    private static SessionResultDto BuildSessionResult(
        VotingSession session,
        string categoryName,
        string projectTitle,
        List<ProjectResultDto> globalRanking)
    {
        bool isClosed = IsSessionClosed(session);

        var result = new SessionResultDto
        {
            SessionId = session.Id,
            SessionName = session.Name,
            IsClosed = isClosed
        };

        if (!isClosed) return result;

        // El dashboard usa la clave "CategoriaNombre|SesionNombre"
        var compositeKey = $"{categoryName}|{session.Name}";

        var sessionRanking = globalRanking
            .Where(r => r.Categoria == compositeKey)
            .OrderByDescending(r => r.Puntos)
            .ToList();

        if (sessionRanking.Count == 0) return result;

        result.TotalRanked = sessionRanking.Count;

        // Los títulos son únicos dentro de un evento (TitleExistsInEventAsync lo garantiza)
        var idx = sessionRanking.FindIndex(r => r.Nombre == projectTitle);
        if (idx >= 0)
        {
            result.Position = idx + 1;
            result.Points = sessionRanking[idx].Puntos;
        }

        return result;
    }

    private static bool IsSessionClosed(VotingSession s)
    {
        // Mismo criterio que el resto del sistema (VotingSession.IsOpen invertido)
        if (s.ManualStatus == "closed") return true;
        if (s.ManualStatus == "paused") return false;
        if (s.ManualStatus == "open") return false;

        var effectiveClose = s.AdjustedCloseAt ?? s.CloseAt;
        if (!s.OpenAt.HasValue || !effectiveClose.HasValue) return false;
        return DateTime.UtcNow > effectiveClose.Value;
    }

    public List<string> GetProjectTypes()
        => new List<string> { "AI", "Sustainability", "General" };

    public List<string> GetMaterialTypes()
        => Enum.GetNames<MaterialType>().ToList();
}