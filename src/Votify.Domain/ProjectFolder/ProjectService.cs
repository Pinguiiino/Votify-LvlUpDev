using Votify.Domain.Factory;

namespace Votify.Domain.ProjectFolder;

public class ProjectService
{
    private readonly IProjectRepository _repository;

    public ProjectService(IProjectRepository repository)
    {
        _repository = repository;
    }

    public async Task<Project> CreateProjectAsync(
        string title, string eventId, string? description,
        string projectType, List<string> categoryIds,
        List<(MaterialType type, string url, string? desc)> materials)
    {
        ProjectCreator creator = projectType switch
        {
            "AI" => new AiProjectCreator(),
            "Sustainability" => new SustainabilityProjectCreator(),
            _ => throw new ArgumentException($"Tipo desconocido: {projectType}")
        };

        var project = creator.Create(title, eventId, description);

        foreach (var (type, url, desc) in materials)
            project.Materials.Add(new ProjectMaterial(project.Id, type, url, desc));

        foreach (var categoryId in categoryIds)
            project.ProjectCategories.Add(new ProjectCategory(project.Id, categoryId));

        await _repository.AddAsync(project);
        await _repository.SaveChangesAsync();
        return project;
    }

    public Task<List<Project>> GetAllAsync() => _repository.GetAllAsync();

    public Task<List<Project>> GetByCategoryAsync(string categoryId)
        => _repository.GetByCategoryAsync(categoryId);

    public List<string> GetProjectTypes()
    => new List<string> { "AI", "Sustainability" };

    public List<string> GetMaterialTypes()
        => Enum.GetNames<MaterialType>().ToList();
}