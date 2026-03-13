using Microsoft.AspNetCore.Mvc;
using Votify.Domain.CategoryFolder;
using Votify.Domain.Factory;
using Votify.Infrastructure;

namespace Votify.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly VotifyDbContext BDcontext;

        public EventsController(VotifyDbContext context)
        {
            BDcontext = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateEvent([FromBody] CreateEventDto request)
        {
            EventCreator eventCreator = new ModalityEventCreator();
            var fechaUtc = DateTime.SpecifyKind(request.StartDate, DateTimeKind.Utc);

            var nuevoEvento = eventCreator.Create(
                name: request.Name,
                maxProjects: request.MaxProjects,
                startDate: fechaUtc,
                modality: request.Modality,
                associatedCategories: new List<Category>(),
                description: request.Description
            );

            if (request.AssociatedCategories != null && request.AssociatedCategories.Any())
            {
                foreach (var cat in request.AssociatedCategories)
                {
                    var nuevaCategoria = new Category(
                        eventId: nuevoEvento.Id,
                        name: cat.Name,
                        criterios: cat.Criterios,
                        description: cat.Description,
                        prizeDescription: cat.PrizeDescription
                    );

                    nuevoEvento.AssociatedCategories.Add(nuevaCategoria);
                }
            }

            BDcontext.Events.Add(nuevoEvento);
            await BDcontext.SaveChangesAsync();

            return Ok(new { message = "Evento creado con éxito" });
        }
    }
    public class CreateEventDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Modality { get; set; } = string.Empty;
        public int MaxProjects { get; set; }
        public DateTime StartDate { get; set; }
        public List<CategoryDto> AssociatedCategories { get; set; } = new();
    }

    public class CategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PrizeDescription { get; set; } = string.Empty;
        public List<double> Criterios { get; set; }
    }
}
