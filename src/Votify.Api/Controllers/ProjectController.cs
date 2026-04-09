using Microsoft.AspNetCore.Mvc;
using Votify.Domain.ProjectFolder;

namespace Votify.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectsController : ControllerBase
    {
        private readonly ProjectService _service;

        public ProjectsController(ProjectService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> CreateProject([FromBody] CreateProjectDto dto)
        {
            try
            {
                var materials = dto.Materials.Select(m => (
                    Enum.TryParse<MaterialType>(m.Type, out var mt) ? mt : MaterialType.Other,
                    m.Url,
                    m.Description
                )).ToList();

                var project = await _service.CreateProjectAsync(
                    dto.Title, dto.EventId, dto.Description,
                    dto.ProjectType, dto.CategoryIds, materials);

                return Ok(new { message = "Proyecto creado", id = project.Id });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var projects = await _service.GetAllAsync();
            return Ok(projects.Select(ToDto));
        }

        [HttpGet("by-category/{categoryId}")]
        public async Task<IActionResult> GetByCategory(string categoryId)
        {
            var projects = await _service.GetByCategoryAsync(categoryId);
            return Ok(projects.Select(ToDto));
        }

        private static object ToDto(Project p) => new
        {
            p.Id,
            p.Title,
            p.Description,
            ProjectType = p.ProjectType(),
            Materials = p.Materials.Select(m => new
            { m.Id, Type = m.Type.ToString(), m.Url, m.Description })
        };

        [HttpGet("project-types")]
        public IActionResult GetProjectTypes()
            => Ok(_service.GetProjectTypes());

        [HttpGet("material-types")]
        public IActionResult GetMaterialTypes()
            => Ok(_service.GetMaterialTypes());
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