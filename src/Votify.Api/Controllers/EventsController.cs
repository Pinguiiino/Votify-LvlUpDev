using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Votify.Domain.CategoryFolder;
using Votify.Domain.EventFolder;
using Votify.Domain.Factory;
using Votify.Infrastructure;

namespace Votify.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly VotifyDbContext Context;

        public EventsController(VotifyDbContext context)
        {
            this.Context = context;
        }

        // GET api/events
        [HttpGet]
        public async Task<IActionResult> GetEvents()
        {
            var eventos = await this.Context.Events
                .AsNoTracking()
                .ToListAsync();

            // Las categorías se cargan aparte porque Event no tiene propiedad de navegación
            var categorias = await this.Context.Categories
                .Include(c => c.Criteria)
                .Include(c => c.Prizes)
                .AsNoTracking()
                .ToListAsync();

            var result = eventos.Select(e => new
            {
                e.Id,
                e.Name,
                e.Description,
                e.MaxProjects,
                e.StartDate,
                e.TopNProjectsAllowed,
                Modality = e.Modality(),
                Categories = categorias
                    .Where(c => c.EventId == e.Id)
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Description,
                        c.AllowSelfVoting,
                        Criteria = c.Criteria.Select(cr => new
                        {
                            cr.Id,
                            cr.Name,
                            cr.Type,
                            cr.Weight,
                            cr.Description
                        }),
                        Prizes = c.Prizes.Select(p => new
                        {
                            p.Id,
                            p.Position,
                            p.Name,
                            p.Description
                        })
                    })
            });

            return Ok(result);
        }

        // POST api/events
        [HttpPost]
        public async Task<IActionResult> CreateEvent([FromBody] CreateEventDto request)
        {
            EventCreator eventCreator = new ModalityEventCreator(request.Modality);

            var startDateUtc = DateTime.SpecifyKind(request.StartDate, DateTimeKind.Utc);
            var endDateUtc = DateTime.SpecifyKind(request.EndDate, DateTimeKind.Utc);

            var nuevoEvento = eventCreator.Create(
                name: request.Name,
                maxProjects: request.MaxProjects,
                startDate: startDateUtc,
                endDate: endDateUtc,
                topNProjectsAllowed: request.TopNProjectsAllowed,
                description: request.Description
            );

            foreach (var catDto in request.Categories)
            {
                var categoria = new Category(
                    eventId: nuevoEvento.Id,
                    name: catDto.Name,
                    description: catDto.Description,
                    allowSelfVoting: catDto.AllowSelfVoting
                );

                // Criterios de evaluación con sus pesos
                foreach (var crDto in catDto.Criteria)
                {
                    var criterionType = Enum.TryParse<CriterionType>(crDto.Type, out var ct) ? ct : CriterionType.Numeric;

                    var criterio = new Criterion(
                        categoryId: categoria.Id,
                        name: crDto.Name,
                        type: criterionType,
                        weight: crDto.Weight,
                        description: crDto.Description
                    );
                    categoria.Criteria.Add(criterio);
                }

                // Premios de la categoría
                foreach (var prDto in catDto.Prizes)
                {
                    var premio = new Prize(
                        categoryId: categoria.Id,
                        position: prDto.Position,
                        name: prDto.Name,
                        description: prDto.Description
                    );
                    categoria.Prizes.Add(premio);
                }

                this.Context.Categories.Add(categoria);
            }

            this.Context.Events.Add(nuevoEvento);
            await this.Context.SaveChangesAsync();

            return Ok(new { message = "Evento creado con éxito", id = nuevoEvento.Id });
        }

        // GET api/events/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEvent(string id)
        {
            var e = await this.Context.Events.AsNoTracking().FirstOrDefaultAsync(ev => ev.Id == id);
            if (e == null) return NotFound();

            var categorias = await this.Context.Categories
                .Include(c => c.Criteria)
                .Include(c => c.Prizes)
                .Where(c => c.EventId == id)
                .AsNoTracking()
                .ToListAsync();

            var result = new
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
            return Ok(result);
        }


    }



    // ── DTOs ────────────────────────────────────────────────────────────────

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
        public bool AllowSelfVoting { get; set; } = false;
        public List<CriterionDto> Criteria { get; set; } = new();
        public List<PrizeDto> Prizes { get; set; } = new();
    }

    public class CriterionDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Type { get; set; } = "Numeric";  // string para evitar problemas de deserialización
        public double Weight { get; set; }
    }

    public class PrizeDto
    {
        public int Position { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}