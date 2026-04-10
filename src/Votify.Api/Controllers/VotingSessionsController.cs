using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Votify.Infrastructure;

namespace Votify.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VotingSessionsController : ControllerBase
    {
        private readonly VotifyDbContext _context;

        public VotingSessionsController(VotifyDbContext context)
        {
            _context = context;
        }

        [HttpGet("active/{eventId}")]
        public async Task<IActionResult> GetActive(string eventId)
        {
            // Buscamos el evento y sus sesiones asociadas
            var evento = await _context.Events
                .Include(e => e.VotingSessions)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (evento == null || !evento.VotingSessions.Any())
                return NotFound("No hay sesión de votación activa");

            // Cogemos la primera sesión de este evento
            var session = evento.VotingSessions.First();

            return Ok(new
            {
                Id = session.Id,
                StartDate = session.OpenAt,
                EndDate = session.CloseAt,
                IsActive = true
            });
        }
    }
}