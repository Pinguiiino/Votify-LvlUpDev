using Votify.Domain.EventFolder;
using Votify.Domain.Factory;

namespace Votify.Domain.ProjectFolder;

public class ProjectService
{
    private readonly IProjectRepository _repository;
    private readonly IEventRepository _eventRepository;

    public ProjectService(IProjectRepository repository, IEventRepository eventRepository)
    {
        _repository = repository;
        _eventRepository = eventRepository;
    }

    public async Task<Project> CreateProjectAsync(
        string title, string eventId, string? description,
        string projectType, string? imageUrl,
        string? ownerId,
        List<string> categoryIds,
        List<(MaterialType type, string url, string? desc)> materials)
    {
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

    public List<string> GetProjectTypes()
        => new List<string> { "AI", "Sustainability", "General" };

    public List<string> GetMaterialTypes()
        => Enum.GetNames<MaterialType>().ToList();
}
