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

    public EventService(
        IEventRepository repository,
        IProjectRepository projectRepository,
        ICategoryRepository categoryRepository,
        IVotingSessionRepository votingSessionRepository,
        IVoteRepository voteRepository,
        IUserRepository userRepository)
    {
        _repository = repository;
        _projectRepository = projectRepository;
        _categoryRepository = categoryRepository;
        _votingSessionRepository = votingSessionRepository;
        _voteRepository = voteRepository;
        _userRepository = userRepository;
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

        var ranking = votosDelEvento
            .GroupBy(v => new { v.VotedProjectId, v.CategoryId, v.VotingSessionId })
            .Select(g =>
            {
                int puntos = 0;

                if (sesionesInfo.TryGetValue(g.Key.VotingSessionId, out var sesion))
                {
                    if (sesion.EvaluationType == EvaluationType.TopN && sesion.TopN.HasValue)
                    {
                        var topN = sesion.TopN.Value;
                        puntos = g.Sum(v => Math.Max(0, (topN - v.TopPosition + 1) * 10));
                    }
                    else
                    {
                        puntos = g.Count() * 10;
                    }
                }

                return new ProjectResultDto
                {
                    Nombre = proyectosInfo.ContainsKey(g.Key.VotedProjectId)
                        ? proyectosInfo[g.Key.VotedProjectId]
                        : "Proyecto",
                    Categoria = categoriasInfo.ContainsKey(g.Key.CategoryId)
                        ? categoriasInfo[g.Key.CategoryId]
                        : "General",
                    Puntos = puntos
                };
            })
            .Where(p => p.Puntos > 0)
            .GroupBy(p => new { p.Nombre, p.Categoria })
            .Select(g => new ProjectResultDto
            {
                Nombre = g.Key.Nombre,
                Categoria = g.Key.Categoria,
                Puntos = g.Sum(x => x.Puntos)
            })
            .OrderByDescending(p => p.Puntos)
            .ToList();

        return new EventDashboardDto
        {
            TotalVotantes = totalRegistrados,
            VotosEmitidos = usuariosQueHanVotado,
            Ranking = ranking
        };
    }
}
