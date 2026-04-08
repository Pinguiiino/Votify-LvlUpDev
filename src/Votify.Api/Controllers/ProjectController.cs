using Microsoft.AspNetCore.Mvc;
using Votify.Domain.Factory;
using Votify.Domain.ProjectFolder;
using Votify.Infrastructure;
using Microsoft.EntityFrameworkCore;

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
        [HttpGet("by-category/{categoryId}")]
        public async Task<IActionResult> GetByCategory(string categoryId)
        {
            var proyectos = await Context.Projects
                .Include(p => p.Materials)
                .Include(p => p.ProjectCategories)
                .Where(p => p.ProjectCategories.Any(pc => pc.CategoryId == categoryId))
                .AsNoTracking()
                .ToListAsync();

            var result = proyectos.Select(p => new
            {
                p.Id,
                p.Title,
                p.Description,
                ProjectType = p.ProjectType(),
                Materials = p.Materials.Select(m => new
                {
                    m.Id,
                    Type = m.Type.ToString(),
                    m.Url,
                    m.Description
                })
            });

            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllProjects()
        {
            var proyectos = await Context.Projects
                .Include(p => p.Materials)
                .Include(p => p.ProjectCategories)
                .AsNoTracking()
                .ToListAsync();

            var result = proyectos.Select(p => new
            {
                p.Id,
                p.Title,
                p.Description,
                ProjectType = p.ProjectType(),
                Materials = p.Materials.Select(m => new
                {
                    m.Id,
                    Type = m.Type.ToString(),
                    m.Url,
                    m.Description
                })
            });

            return Ok(result);
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