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

    public async Task<Event> CreateEventAsync(
        string name, string modality, int maxProjects,
        DateTime startDate, DateTime endDate,
        string? description,
        string? imageUrl,
        string organizerId)
    {
        bool nombreOcupado = await _repository.ExistsByNameAsync(name);
        if (nombreOcupado)
            throw new ArgumentException($"Ya existe un evento con el nombre \"{name}\". Elige un nombre diferente.");

        var creator = new ModalityEventCreator(modality);
        var evento = creator.Create(name, maxProjects, startDate, endDate, description, imageUrl);

        evento.Organizer = organizerId;
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

        var rankingTopN = votosDelEvento
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
                return new { g.Key.VotedProjectId, g.Key.CategoryId, puntos };
            })
            .Where(x => x.puntos > 0)
            .ToList();

        var weightedSessionIds = sesiones
            .Where(vs => vs.EvaluationType == EvaluationType.WeightedScale)
            .Select(vs => vs.Id)
            .ToList();

        var rankingWeighted = new List<(string ProjectId, string CategoryId, double Score)>();

        if (weightedSessionIds.Any())
        {
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

                var scoresPorProyecto = votosDeSesion
                    .GroupBy(wv => wv.ProjectId)
                    .Select(g => (
                        ProjectId: g.Key,
                        CategoryId: categoryId,
                        Score: g.Average(wv =>
                            wv.CriterionScores.Sum(cs =>
                                cs.Score * (weightMap.TryGetValue(cs.CriterionId, out var w) ? w : 0)))
                    ));

                rankingWeighted.AddRange(scoresPorProyecto);
            }
        }

        var ranking = new List<ProjectResultDto>();

        foreach (var grupo in rankingTopN.GroupBy(x => new { x.VotedProjectId, x.CategoryId }))
        {
            ranking.Add(new ProjectResultDto
            {
                Nombre = proyectosInfo.GetValueOrDefault(grupo.Key.VotedProjectId, "Proyecto"),
                Categoria = categoriasInfo.GetValueOrDefault(grupo.Key.CategoryId, "General"),
                Puntos = grupo.Sum(x => x.puntos)
            });
        }

        foreach (var grupo in rankingWeighted.GroupBy(x => new { x.ProjectId, x.CategoryId }))
        {
            var puntos = (int)Math.Round(grupo.Sum(x => x.Score) * 10);
            var existente = ranking.FirstOrDefault(r =>
                r.Nombre == proyectosInfo.GetValueOrDefault(grupo.Key.ProjectId, "") &&
                r.Categoria == categoriasInfo.GetValueOrDefault(grupo.Key.CategoryId, ""));

            if (existente != null)
                existente.Puntos += puntos;
            else
                ranking.Add(new ProjectResultDto
                {
                    Nombre = proyectosInfo.GetValueOrDefault(grupo.Key.ProjectId, "Proyecto"),
                    Categoria = categoriasInfo.GetValueOrDefault(grupo.Key.CategoryId, "General"),
                    Puntos = puntos
                });
        }

        return new EventDashboardDto
        {
            TotalVotantes = totalRegistrados,
            VotosEmitidos = usuariosQueHanVotado,
            Ranking = ranking.OrderByDescending(p => p.Puntos).ToList()
        };
    }
}
