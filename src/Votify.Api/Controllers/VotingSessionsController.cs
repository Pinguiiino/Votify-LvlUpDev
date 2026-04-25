using Microsoft.AspNetCore.Mvc;
using Votify.Domain.VoteFolder;

namespace Votify.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VotingSessionsController : ControllerBase
    {
        private readonly VotingSessionService _service;

        public VotingSessionsController(VotingSessionService service)
        {
            _service = service;
        }

        [HttpGet("active/{eventId}")]
        public async Task<IActionResult> GetActive(string eventId)
        {
            var sesiones = await _service.GetByEventAsync(eventId);

            if (sesiones.Count == 0)
                return NotFound("No hay sesiones de votación para este evento.");

            var now = DateTime.UtcNow;
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
