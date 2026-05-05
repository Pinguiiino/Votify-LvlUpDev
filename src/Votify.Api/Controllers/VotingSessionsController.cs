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
                Name = vs.Name,
                Description = vs.Description,
                VoterType = vs.VoterType.ToString(),
                EvaluationType = vs.EvaluationType.ToString(),
                vs.AllowComments,
                vs.RequireComments,
                vs.AllowCommentsPerCriterion,
                StartDate = vs.OpenAt,
                EndDate = vs.EffectiveCloseAt,
                OpenAt = vs.OpenAt.Equals(default) ? (DateTime?)null : vs.OpenAt,
                CloseAt = vs.CloseAt.Equals(default) ? (DateTime?)null : vs.CloseAt,
                ManualStatus = vs.ManualStatus,
                IsActive = vs.IsOpen
            });

            return Ok(result);
        }

        [HttpGet("by-category/{categoryId}")]
        public async Task<IActionResult> GetByCategory(string categoryId)
        {
            var sesiones = await _service.GetByCategoryAsync(categoryId);
            return Ok(sesiones.Select(vs => new
            {
                vs.Id,
                VoterType = vs.VoterType.ToString(),
                EvaluationType = vs.EvaluationType.ToString(),
                CriterionType = vs.CriterionType?.ToString(),
                vs.AllowComments,
                vs.RequireComments,
                vs.AllowCommentsPerCriterion,
                vs.PointsPerVoter,
                vs.MaxPointsPerProject,
                vs.TopN,
                Criteria = vs.Criteria.Select(cr => new { cr.Id, cr.Name, cr.Description, cr.Weight })
            }));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateWindow(string id, [FromBody] UpdateVotingWindowDto dto)
        {
            var session = await _service.GetByIdAsync(id);
            if (session == null)
                return NotFound("Sesión de votación no encontrada.");

            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("El nombre de la votación es obligatorio.");

            if (dto.OpenAt.HasValue && dto.OpenAt.Value < DateTime.UtcNow)
                return BadRequest("La fecha de apertura no puede ser anterior a la fecha actual.");

            if (dto.OpenAt.HasValue && dto.CloseAt.HasValue && dto.CloseAt <= dto.OpenAt)
                return BadRequest("La fecha de cierre debe ser posterior a la de apertura.");

            session.Name = dto.Name;
            session.Description = dto.Description;
            session.OpenAt = dto.OpenAt ?? session.OpenAt;

            if (dto.CloseAt.HasValue)
            {
                session.AdjustedCloseAt = dto.CloseAt;
                session.CloseAt = dto.CloseAt.Value;
            }

            await _service.UpdateAsync(session);

            return Ok(new { message = "Ventana de votación actualizada." });
        }

        [HttpPost("{id}/manual-status")]
        public async Task<IActionResult> SetManualStatus(string id, [FromBody] ManualStatusDto dto)
        {
            var session = await _service.GetByIdAsync(id);
            if (session == null)
                return NotFound("Sesión de votación no encontrada.");

            var now = DateTime.UtcNow;
            switch (dto.Action?.ToLower())
            {
                case "open":
                    if (session.OpenAt > now)
                        session.OpenAt = now;
                    session.ManualStatus = "open";
                    session.IsManuallyAdjusted = true;
                    break;

                case "pause":
                    session.ManualStatus = "paused";
                    session.IsManuallyAdjusted = true;
                    break;

                case "resume":
                    session.ManualStatus = "open";
                    session.IsManuallyAdjusted = true;
                    break;

                case "close":
                    session.AdjustedCloseAt = now;
                    session.ManualStatus = "closed";
                    session.IsManuallyAdjusted = true;
                    break;

                default:
                    return BadRequest("Acción no reconocida. Use: open, pause, resume, close.");
            }

            await _service.UpdateAsync(session);

            return Ok(new { message = $"Sesión marcada como '{dto.Action}'." });
        }
    }

    public class UpdateVotingWindowDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? OpenAt { get; set; }
        public DateTime? CloseAt { get; set; }
    }

    public class ManualStatusDto
    {
        public string Action { get; set; } = string.Empty;
    }
}
