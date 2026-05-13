using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Votify.Domain.EventFolder;

namespace Votify.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly EventService _service;
    private readonly IWebHostEnvironment _env;

    public EventsController(EventService service, IWebHostEnvironment env)
    {
        _service = service;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> GetEvents()
    {
        var eventos = await _service.GetAllAsync();
        var result = new List<object>();

        foreach (var e in eventos)
        {
            var categorias = await _service.GetCategoriesWithDetailsAsync(e.Id);
            result.Add(ToDto(e, categorias));
        }

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetEvent(string id)
    {
        var evento = await _service.GetByIdAsync(id);
        if (evento == null) return NotFound();

        var categorias = await _service.GetCategoriesWithDetailsAsync(id);
        return Ok(ToDto(evento, categorias));
    }

    [HttpPost]
    [Authorize(Roles = "Organizer")]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventDto dto)
    {
        try
        {
            var organizerId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(organizerId))
            {
                return Unauthorized("No se pudo identificar al usuario organizador.");
            }

            var fechaInicioUtc = dto.StartDate.ToUniversalTime();
            var fechaFinUtc = dto.EndDate.ToUniversalTime();

            var evt = await _service.CreateEventAsync(
                dto.Name,
                dto.Modality,
                dto.MaxProjects,
                fechaInicioUtc,
                fechaFinUtc,
                dto.Description,
                dto.ImageUrl,
                organizerId,
                dto.AuditorEmail);

            return Ok(evt);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            var errorReal = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            return StatusCode(500, errorReal);
        }
    }


    [HttpPost("{eventId}/enroll")]
    public async Task<IActionResult> EnrollInEvent(string eventId, [FromBody] EnrollRequestDto dto)
    {
        try
        {
            await _service.EnrollUserAsync(eventId, dto.UserId, dto.Role);
            return Ok(new { message = "Inscripción exitosa" });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    

    [HttpGet("{id}/stats")]
    public async Task<ActionResult<EventDashboardDto>> GetStats(string id)
    {
        try
        {
            var stats = await _service.GetDashboardStatsAsync(id);
            if (stats == null) return NotFound("Evento no encontrado");
            return Ok(stats);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error al obtener estadísticas: {ex.Message}");
        }
    }

    [HttpPost("{eventId}/assign-auditor")]
    [Authorize(Roles = "Organizer")]
    public async Task<IActionResult> AssignAuditor(string eventId, [FromBody] AssignAuditorDto dto)
    {
        try
        {
            await _service.AssignAuditorAsync(eventId, dto.AuditorId);
            return Ok(new { message = "Auditor asignado correctamente" });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
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

        var folder = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "events");
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(folder, fileName);

        using (var stream = new FileStream(fullPath, FileMode.Create))
            await file.CopyToAsync(stream);

        var url = $"/uploads/events/{fileName}";
        return Ok(new { url });
    }

    private static object ToDto(Event e, List<Votify.Domain.CategoryFolder.Category> categorias) => new
    {
        e.Id,
        e.Name,
        e.Description,
        e.MaxProjects,
        e.StartDate,
        e.EndDate,
        e.ImageUrl,
        Modality = e.Modality(),

        Organizer = e.Organizer ?? string.Empty,
        AuditorId = e.Auditor ?? string.Empty,
        ParticipantsIds = e.Participants?.Select(p => p.Id).ToList() ?? new List<string>(),
        PublicIds = e.Public?.Select(p => p.Id).ToList() ?? new List<string>(),

        Categories = categorias.Select(c => new
        {
            c.Id,
            c.Name,
            c.Description,
            c.AllowSelfVoting,
            c.CombineResults,
            c.JuryWeight,
            c.PublicWeight,
            VotingSessions = c.VotingSessions.Select(vs => new
            {
                vs.Id,
                vs.Name,
                vs.Description,
                VoterType = vs.VoterType.ToString(),
                EvaluationType = vs.EvaluationType.ToString(),
                CriterionType = vs.CriterionType?.ToString(),
                vs.TopN,
                vs.PointsPerVoter,
                vs.MaxPointsPerProject,
                vs.AllowComments,
                vs.RequireComments,
                vs.AllowCommentsPerCriterion,
                vs.OpenAt,
                vs.CloseAt,
                vs.ManualStatus,
                Criteria = vs.Criteria.Select(cr => new { cr.Id, cr.Name, cr.Weight, cr.Description }),
                JurorEmails = vs.JurorEmails,
                Prizes = vs.Prizes.Select(p => new
                {
                    p.Id,
                    p.Position,
                    p.Name,
                    p.Description
                })
            })
        })
    };
}

public class CreateEventDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Modality { get; set; } = string.Empty;
    public int MaxProjects { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? ImageUrl { get; set; }
    public string OrganizerId { get; set; } = string.Empty;
    public string AuditorEmail { get; set; } = string.Empty;
}
public class EnrollRequestDto
{
    public string UserId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class AssignAuditorDto
{
    public string AuditorId { get; set; } = string.Empty;
}