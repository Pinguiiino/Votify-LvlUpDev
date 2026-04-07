using Microsoft.AspNetCore.Mvc;
using Votify.Domain.Factory;
using Votify.Domain.ProjectFolder;
using Votify.Infrastructure;

namespace Votify.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectsController : ControllerBase
    {
        private readonly VotifyDbContext Context;

        public ProjectsController(VotifyDbContext context)
        {
            this.Context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateProject([FromBody] CreateProjectDto request)
        {
            ProjectCreator? creator = request.ProjectType switch
            {
                "AI" => new AiProjectCreator(),
                "Sustainability" => new SustainabilityProjectCreator(),
                _ => null
            };

            if (creator == null)
                return BadRequest($"Tipo de proyecto desconocido: {request.ProjectType}");

            var proyecto = creator.Create(request.Title, request.EventId, request.Description);

            foreach (var matDto in request.Materials)
            {
                var material = new ProjectMaterial(
                    projectId: proyecto.Id,
                    type: Enum.TryParse<MaterialType>(matDto.Type, out var mt) ? mt : MaterialType.Other,
                    url: matDto.Url,
                    description: matDto.Description
                );
                proyecto.Materials.Add(material);
            }

            foreach (var categoryId in request.CategoryIds)
            {
                var projectCategory = new ProjectCategory(proyecto.Id, categoryId);
                proyecto.ProjectCategories.Add(projectCategory);
            }

            this.Context.Projects.Add(proyecto);
            await this.Context.SaveChangesAsync();

            return Ok(new { message = "Proyecto creado con éxito", id = proyecto.Id });
        }


    }

    public class CreateProjectDto
    {
        public string Title { get; set; } = string.Empty;
        public string EventId { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string ProjectType { get; set; } = "AI";
        public List<string> CategoryIds { get; set; } = new();
        public List<MaterialDto> Materials { get; set; } = new();
    }

    public class MaterialDto
    {
        public string Type { get; set; } = "Other";
        public string Url { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}