using Votify.Domain.CategoryFolder;
using Votify.Domain.Factory;
using Votify.Domain.ProjectFolder;
using Votify.Domain.UserFolder;
using Votify.Domain.VoteFolder;

namespace Votify.Domain.EventFolder;

public class EventService
{
    private readonly IEventRepository _repository;
    private readonly IProjectRepository _projectRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IVotingSessionRepository _votingSessionRepository;
    private readonly IVoteRepository _voteRepository;
    private readonly IUserRepository _userRepository;
    private readonly IWeightedVoteRepository _weightedVoteRepository;

    public EventService(
        IEventRepository repository,
        IProjectRepository projectRepository,
        ICategoryRepository categoryRepository,
        IVotingSessionRepository votingSessionRepository,
        IVoteRepository voteRepository,
        IUserRepository userRepository,
        IWeightedVoteRepository weightedVoteRepository)
    {
        _repository = repository;
        _projectRepository = projectRepository;
        _categoryRepository = categoryRepository;
        _votingSessionRepository = votingSessionRepository;
        _voteRepository = voteRepository;
        _userRepository = userRepository;
        _weightedVoteRepository = weightedVoteRepository;
    }

    public Task<List<Event>> GetAllAsync() => _repository.GetAllAsync();

    public Task<Event?> GetByIdAsync(string id) => _repository.GetByIdAsync(id);

    public Task<List<Category>> GetCategoriesWithDetailsAsync(string eventId)
        => _repository.GetCategoriesWithDetailsAsync(eventId);

    public async Task<Event> CreateEventAsync(EventData data, string organizerId)
    {
        bool nombreOcupado = await _repository.ExistsByNameAsync(data.Name);
        if (nombreOcupado)
            throw new ArgumentException($"Ya existe un evento con el nombre \"{data.Name}\".");

        var auditor = await _userRepository.GetByEmailAsync(data.AuditorEmail)
            ?? throw new ArgumentException($"No existe ninguna cuenta con el correo '{data.AuditorEmail}'.");

        var creator = new ModalityEventCreator(data.Modality);
        var evento = creator.Create(data.Name, data.MaxProjects, data.StartDate, data.EndDate,
                                    data.Description, data.ImageUrl);

        evento.Organizer = organizerId;
        evento.Auditor = auditor.Id;
        evento.Participants ??= new List<GeneralUser>();
        evento.Public ??= new List<GeneralUser>();

        await _repository.AddAsync(evento);
        await _repository.SaveChangesAsync();
        return evento;
    }

    public async Task EnrollUserAsync(string eventId, string userId, string role)
    {
        var evt = await _repository.GetByIdAsync(eventId);
        if (evt == null) throw new ArgumentException("Evento no encontrado.");

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || user is not GeneralUser generalUser)
            throw new ArgumentException("Usuario no encontrado o no es válido para inscripción.");

        evt.Participants ??= new List<GeneralUser>();
        evt.Public ??= new List<GeneralUser>();

        if (role == "Participant")
        {
            if (!evt.Participants.Any(p => p.Id == userId))
            {
                evt.Participants.Add(generalUser);
            }
        }
        else if (role == "Public")
        {
            if (!evt.Public.Any(p => p.Id == userId))
            {
                evt.Public.Add(generalUser);
            }
        }
        else
        {
            throw new ArgumentException("Rol no válido para inscripción.");
        }

        await _repository.SaveChangesAsync();
    }

    public async Task<EventDashboardDto?> GetDashboardStatsAsync(string eventId)
    {
        var evento = await _repository.GetByIdAsync(eventId);
        if (evento == null) return null;

        var proyectos = await _projectRepository.GetByEventAsync(eventId);
        var proyectosInfo = proyectos.ToDictionary(p => p.Id, p => p.Title);
        var categorias = await _categoryRepository.GetByEventAsync(eventId);
        var categoriasInfo = categorias.ToDictionary(c => c.Id, c => c.Name);
        var sesiones = await _votingSessionRepository.GetByEventAsync(eventId);
        var sesionesInfo = sesiones.ToDictionary(vs => vs.Id, vs => vs);
        var projectIds = proyectosInfo.Keys.ToList();
        var votosDelEvento = await _voteRepository.GetByProjectIdsAsync(projectIds);

        var usuariosQueHanVotado = votosDelEvento.Select(v => v.UserId).Distinct().Count();
        var totalRegistrados = await _userRepository.CountAsync();

        var rankingTopN = BuildTopNRanking(votosDelEvento, sesionesInfo);
        var rankingWeighted = await BuildWeightedRankingAsync(sesiones);
        var ranking = CombineRankings(rankingTopN, rankingWeighted, proyectosInfo, categoriasInfo, sesionesInfo);

        return new EventDashboardDto
        {
            TotalVotantes = totalRegistrados,
            VotosEmitidos = usuariosQueHanVotado,
            Ranking = ranking.OrderByDescending(p => p.Puntos).ToList()
        };
    }

    private static List<TopNEntry> BuildTopNRanking(
    List<Vote> votos,
    Dictionary<string, VotingSession> sesionesInfo)
    {
        return votos
            .GroupBy(v => new { v.VotedProjectId, v.CategoryId, v.VotingSessionId })
            .Select(g =>
            {
                int puntos = 0;
                if (sesionesInfo.TryGetValue(g.Key.VotingSessionId, out var sesion))
                {
                    if (sesion.EvaluationType == EvaluationType.TopN && sesion.TopN.HasValue)
                        puntos = g.Sum(v => Math.Max(0, (sesion.TopN.Value - v.TopPosition + 1) * 10));
                    else if (sesion.EvaluationType == EvaluationType.PointDistribution)
                        puntos = g.Sum(v => v.Points ?? 0);
                }
                return new TopNEntry(g.Key.VotedProjectId, g.Key.CategoryId, g.Key.VotingSessionId, puntos);
            })
            .Where(x => x.Puntos > 0)
            .ToList();
    }

    private async Task<List<(string ProjectId, string CategoryId, string VotingSessionId, double Score)>>
        BuildWeightedRankingAsync(List<VotingSession> sesiones)
    {
        var result = new List<(string, string, string, double)>();
        var weightedSessionIds = sesiones
            .Where(vs => vs.EvaluationType == EvaluationType.WeightedScale)
            .Select(vs => vs.Id)
            .ToList();

        if (!weightedSessionIds.Any()) return result;

        var weightedVotes = await _weightedVoteRepository.GetBySessionIdsAsync(weightedSessionIds);
        var criteriaBySession = sesiones
            .Where(vs => vs.EvaluationType == EvaluationType.WeightedScale)
            .ToDictionary(vs => vs.Id, vs => vs.Criteria);

        foreach (var sessionId in weightedSessionIds)
        {
            var criteria = criteriaBySession[sessionId];
            if (!criteria.Any()) continue;

            var weightMap = criteria.ToDictionary(c => c.Id, c => c.Weight);
            var categoryId = sesiones.First(vs => vs.Id == sessionId).CategoryId;
            var votosDeSesion = weightedVotes.Where(wv => wv.VotingSessionId == sessionId).ToList();

            var scores = votosDeSesion
                .GroupBy(wv => wv.ProjectId)
                .Select(g => (
                    ProjectId: g.Key,
                    CategoryId: categoryId,
                    VotingSessionId: sessionId,
                    Score: g.Average(wv =>
                        wv.CriterionScores.Sum(cs =>
                            cs.Score * (weightMap.TryGetValue(cs.CriterionId, out var w) ? w : 0)))
                ));

            result.AddRange(scores);
        }

        return result;
    }

    private static List<ProjectResultDto> CombineRankings(
        List<TopNEntry> topNRanking,
        List<(string ProjectId, string CategoryId, string VotingSessionId, double Score)> weightedRanking,
        Dictionary<string, string> proyectosInfo,
        Dictionary<string, string> categoriasInfo,
        Dictionary<string, VotingSession> sesionesInfo)
    {
        var ranking = new List<ProjectResultDto>();

        foreach (var grupo in topNRanking.GroupBy(x => new { x.VotedProjectId, x.CategoryId, x.VotingSessionId }))
        {
            string catNombre = categoriasInfo.GetValueOrDefault(grupo.Key.CategoryId, "General");
            string sesionNombre = sesionesInfo.GetValueOrDefault(grupo.Key.VotingSessionId)?.Name ?? "Votación";
            ranking.Add(new ProjectResultDto
            {
                Nombre = proyectosInfo.GetValueOrDefault(grupo.Key.VotedProjectId, "Proyecto"),
                Categoria = $"{catNombre}|{sesionNombre}",
                Puntos = grupo.Sum(x => x.Puntos)
            });
        }

        foreach (var grupo in weightedRanking.GroupBy(x => new { x.ProjectId, x.CategoryId, x.VotingSessionId }))
        {
            var puntos = (int)Math.Round(grupo.Sum(x => x.Score) * 10);
            string catNombre = categoriasInfo.GetValueOrDefault(grupo.Key.CategoryId, "General");
            string sesionNombre = sesionesInfo.GetValueOrDefault(grupo.Key.VotingSessionId)?.Name ?? "Votación";
            string tituloCompuesto = $"{catNombre}|{sesionNombre}";

            var existente = ranking.FirstOrDefault(r =>
                r.Nombre == proyectosInfo.GetValueOrDefault(grupo.Key.ProjectId, "") &&
                r.Categoria == tituloCompuesto);

            if (existente != null)
                existente.Puntos += puntos;
            else
                ranking.Add(new ProjectResultDto
                {
                    Nombre = proyectosInfo.GetValueOrDefault(grupo.Key.ProjectId, "Proyecto"),
                    Categoria = tituloCompuesto,
                    Puntos = puntos
                });
        }

        return ranking;
    }

    public async Task AssignAuditorAsync(string eventId, string auditorId)
    {
        var evento = await _repository.GetByIdAsync(eventId);
        if (evento == null)
            throw new ArgumentException("Evento no encontrado.");

        evento.Auditor = auditorId;
        await _repository.SaveChangesAsync();
    }

    public async Task<Event> UpdateEventAsync(string id, EventData data)
    {
        var evento = await _repository.GetByIdAsync(id)
            ?? throw new ArgumentException("Evento no encontrado.");

        if (DateTime.UtcNow >= evento.StartDate)
            throw new ArgumentException("No se puede editar el evento porque ya ha comenzado.");

        if (evento.Name != data.Name)
        {
            bool nombreOcupado = await _repository.ExistsByNameAsync(data.Name);
            if (nombreOcupado)
                throw new ArgumentException($"Ya existe un evento con el nombre \"{data.Name}\".");
        }

        var auditor = await _userRepository.GetByEmailAsync(data.AuditorEmail)
            ?? throw new ArgumentException($"No existe ninguna cuenta con el correo '{data.AuditorEmail}'.");

        evento.Name = data.Name;
        evento.SetModality(data.Modality);
        evento.MaxProjects = data.MaxProjects;
        evento.StartDate = data.StartDate;
        evento.EndDate = data.EndDate;
        evento.Description = data.Description;
        evento.ImageUrl = data.ImageUrl;
        evento.Auditor = auditor.Id;

        await _repository.SaveChangesAsync();
        return evento;
    }

    public async Task<string> GetUserEmailByIdAsync(string id)
    {
        if (string.IsNullOrEmpty(id)) return string.Empty;
        var user = await _userRepository.GetByIdAsync(id);
        return user?.Email ?? string.Empty;
    }

    private record TopNEntry(string VotedProjectId, string CategoryId, string VotingSessionId, int Puntos);
}