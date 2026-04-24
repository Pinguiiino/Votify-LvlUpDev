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
            var now = DateTime.UtcNow;

            var sesiones = await _context.VotingSessions
                .Include(vs => vs.Category)
                .Where(vs => vs.Category != null && vs.Category.EventId == eventId)
                .ToListAsync();

            if (sesiones.Count == 0)
                return NotFound("No hay sesiones de votación para este evento.");

            var result = sesiones.Select(vs => new
            {
                vs.Id,
                CategoryId = vs.CategoryId,
                CategoryName = vs.Category!.Name,
                VoterType = vs.VoterType.ToString(),
                EvaluationType = vs.EvaluationType.ToString(),
                StartDate = vs.OpenAt,
                EndDate = vs.EffectiveCloseAt,
                IsActive = now >= vs.OpenAt && now <= vs.EffectiveCloseAt
            });

            return Ok(result);
        }
    }
}
