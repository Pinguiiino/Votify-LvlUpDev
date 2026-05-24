using Microsoft.AspNetCore.Mvc;
using Votify.Domain.ProjectFolder;

namespace Votify.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectsController : ControllerBase
    {
        private readonly ProjectService _service;
        private readonly IWebHostEnvironment _env;

        public ProjectsController(ProjectService service, IWebHostEnvironment env)
        {
            _service = service;
            _env = env;
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
                    dto.ProjectType, dto.ImageUrl,
                    dto.OwnerId,
                    dto.CategoryIds, materials);

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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var project = await _service.GetByIdAsync(id);
            if (project == null) return NotFound();
            return Ok(ToDto(project));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateProjectDto dto)
        {
            try
            {
                var materials = dto.Materials.Select(m => (
                    Enum.TryParse<MaterialType>(m.Type, out var mt) ? mt : MaterialType.Other,
                    m.Url,
                    m.Description
                )).ToList();

                var project = await _service.UpdateProjectAsync(
                    id, dto.RequesterId ?? string.Empty,
                    dto.Description, dto.ImageUrl, materials);

                return Ok(new { message = "Proyecto actualizado", id = project.Id });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("by-category/{categoryId}")]
        public async Task<IActionResult> GetByCategory(string categoryId)
        {
            var projects = await _service.GetByCategoryAsync(categoryId);
            return Ok(projects.Select(ToDto));
        }

        [HttpGet("by-owner/{ownerId}")]
        public async Task<IActionResult> GetByOwner(string ownerId)
        {
            var projects = await _service.GetByOwnerAsync(ownerId);
            return Ok(projects.Select(ToDto));
        }

        [HttpGet("pending/by-event/{eventId}")]
        public async Task<IActionResult> GetPendingByEvent(string eventId)
        {
            var projects = await _service.GetPendingByEventAsync(eventId);
            return Ok(projects.Select(ToPendingDto));
        }

        [HttpPost("{id}/approve")]
        public async Task<IActionResult> Approve(string id, [FromBody] ValidationRequestDto dto)
        {
            try
            {
                await _service.ApproveAsync(id, dto.RequesterId ?? string.Empty);
                return Ok(new { message = "Proyecto aprobado", id });
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        }

        [HttpPost("{id}/reject")]
        public async Task<IActionResult> Reject(string id, [FromBody] RejectProjectDto dto)
        {
            try
            {
                await _service.RejectAsync(id, dto.RequesterId ?? string.Empty, dto.Reason);
                return Ok(new { message = "Proyecto rechazado", id });
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        }

        [HttpPost("upload-image")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No se ha proporcionado ningún archivo.");

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
                return BadRequest("Formato no permitido. Usa jpg, png, webp o gif.");

            var folder = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "projects");
            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
                await file.CopyToAsync(stream);

            var url = $"/uploads/projects/{fileName}";
            return Ok(new { url });
        }

        private static object ToDto(Project p) => new
        {
            p.Id,
            p.Title,
            p.Description,
            p.EventId,
            p.OwnerId,
            p.ImageUrl,
            ProjectType = p.ProjectType(),
            ValidationStatus = p.ValidationStatus.ToString(),
            p.RejectionReason,
            Materials = p.Materials.Select(m => new
            { m.Id, Type = m.Type.ToString(), m.Url, m.Description })
        };

        private static object ToPendingDto(Project p) => new
        {
            p.Id,
            p.Title,
            p.Description,
            p.EventId,
            p.OwnerId,
            p.ImageUrl,
            ProjectType = p.ProjectType(),
            Categories = p.ProjectCategories
                .Where(pc => pc.Category != null)
                .Select(pc => new { pc.CategoryId, Name = pc.Category!.Name })
        };

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, [FromBody] DeleteProjectDto dto)
        {
            try
            {
                await _service.DeleteAsync(id, dto.RequesterId ?? string.Empty);
                return Ok(new { message = "Proyecto eliminado correctamente." });
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.InnerException?.Message ?? ex.Message);
            }
        }

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
        public string? ImageUrl { get; set; }
        public string? OwnerId { get; set; }
        public List<string> CategoryIds { get; set; } = new();
        public List<MaterialDto> Materials { get; set; } = new();
    }

    public class UpdateProjectDto
    {
        public string? RequesterId { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public List<MaterialDto> Materials { get; set; } = new();
    }

    public class MaterialDto
    {
        public string Type { get; set; } = "Other";
        public string Url { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class ValidationRequestDto
    {
        public string? RequesterId { get; set; }
    }

    public class RejectProjectDto
    {
        public string? RequesterId { get; set; }
        public string? Reason { get; set; }
    }

    public class DeleteProjectDto
    {
        public string? RequesterId { get; set; }
    }
}
