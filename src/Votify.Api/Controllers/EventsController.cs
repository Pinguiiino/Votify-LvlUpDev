using Microsoft.AspNetCore.Mvc;
using Votify.Domain.EventFolder;

namespace Votify.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly EventService _service;

    public EventsController(EventService service)
    {
        _service = service;
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
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventDto dto)
    {
        try
        {
            var startUtc = DateTime.SpecifyKind(dto.StartDate, DateTimeKind.Utc);
            var endUtc = DateTime.SpecifyKind(dto.EndDate, DateTimeKind.Utc);

            var categoriasData = dto.Categories.Select(c => new CreateCategoryData
            {
                Name = c.Name,
                Description = c.Description,
                AllowSelfVoting = c.AllowSelfVoting,
                Criteria = c.Criteria.Select(cr => new CreateCriterionData
                {
                    Name = cr.Name,
                    Description = cr.Description,
                    Type = cr.Type,
                    Weight = cr.Weight
                }).ToList(),
                Prizes = c.Prizes.Select(p => new CreatePrizeData
                {
                    Position = p.Position,
                    Name = p.Name,
                    Description = p.Description
                }).ToList()
            }).ToList();

            var evento = await _service.CreateEventAsync(
                dto.Name, dto.Modality, dto.MaxProjects,
                startUtc, endUtc, dto.TopNProjectsAllowed,
                dto.Description, categoriasData);

            return Ok(new { message = "Evento creado con éxito", id = evento.Id });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private static object ToDto(Event e, List<Votify.Domain.CategoryFolder.Category> categorias) => new
    {
        e.Id,
        e.Name,
        e.Description,
        e.MaxProjects,
        e.StartDate,
        e.EndDate,
        e.TopNProjectsAllowed,
        Modality = e.Modality(),
        Categories = categorias.Select(c => new
        {
            c.Id,
            c.Name,
            c.Description,
            c.AllowSelfVoting,
            Criteria = c.Criteria.Select(cr => new { cr.Id, cr.Name, cr.Type, cr.Weight, cr.Description }),
            Prizes = c.Prizes.Select(p => new { p.Id, p.Position, p.Name, p.Description })
        })
    };
}

// DTOs
public class CreateEventDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Modality { get; set; } = string.Empty;
    public int MaxProjects { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TopNProjectsAllowed { get; set; }
    public List<CategoryDto> Categories { get; set; } = new();
}

public class CategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool AllowSelfVoting { get; set; }
    public List<CriterionDto> Criteria { get; set; } = new();
    public List<PrizeDto> Prizes { get; set; } = new();
}

public class CriterionDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = "Numeric";
    public double Weight { get; set; }
}

public class PrizeDto
{
    public int Position { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}