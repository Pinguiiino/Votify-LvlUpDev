using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Votify.Domain.AuditFolder;
using Votify.Domain.EventFolder;
using Votify.Domain.ProjectFolder;

namespace Votify.Api.Controllers
{
    [ApiController]
    [Route("api/audit")]
    public class AuditController : ControllerBase
    {
        private readonly AuditService _service;
        private readonly EventService _eventService;
        private readonly ProjectService _projectService;

        public AuditController(AuditService service, EventService eventService, ProjectService projectService)
        {
            _service = service;
            _eventService = eventService;
            _projectService = projectService;
        }

        [HttpGet("project/{projectId}")]
        [Authorize]
        public async Task<IActionResult> GetProjectAudit(string projectId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!User.IsInRole("Auditor"))
            {
                var proyecto = await _projectService.GetByIdAsync(projectId);
                if (proyecto == null) return NotFound();
                var evento = await _eventService.GetByIdAsync(proyecto.EventId);
                if (evento == null) return NotFound();
                if (evento.Auditor != userId) return Forbid();
            }

            var auditTrail = await _service.GetProjectAuditAsync(projectId);
            var result = auditTrail.Select(a => new
            {
                voter = a.Voter,
                date = a.Date,
                hash = a.Hash,
                topPosition = a.TopPosition,
                comment = a.Comment
            });
            return Ok(result);
        }

        [HttpPost("request/{projectId}")]
        [Authorize]
        public async Task<IActionResult> RequestAudit(string projectId)
        {
            await _service.RequestAuditAsync(projectId);
            return Ok(new { message = "Solicitud registrada." });
        }

        [HttpGet("requested")]
        [Authorize]
        public async Task<IActionResult> GetRequestedProjectIds()
        {
            if (!User.IsInRole("Auditor")) return Forbid();
            var ids = await _service.GetRequestedProjectIdsAsync();
            return Ok(ids);
        }

        [HttpGet("dashboard/{eventId}")]
        [Authorize]
        public async Task<IActionResult> GetAuditDashboardByEvent(string eventId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!User.IsInRole("Auditor"))
            {
                var evento = await _eventService.GetByIdAsync(eventId);
                if (evento == null) return NotFound();
                if (evento.Auditor != userId) return Forbid();
            }

            var requests = await _service.GetDashboardByEventAsync(eventId);
            var result = requests.Select(r => new
            {
                r.AuditId,
                r.ProjectId,
                r.ProjectTitle,
                r.RequestedAt
            });
            return Ok(result);
        }
    }
}
