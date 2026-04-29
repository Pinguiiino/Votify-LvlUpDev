using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Votify.Domain.AuditFolder;

namespace Votify.Api.Controllers
{
    [ApiController]
    [Route("api/audit")]
    public class AuditController : ControllerBase
    {
        private readonly AuditService _service;

        public AuditController(AuditService service)
        {
            _service = service;
        }

        [HttpGet("project/{projectId}")]
        [Authorize(Roles = "Auditor")]
        public async Task<IActionResult> GetProjectAudit(string projectId)
        {
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
        [Authorize(Roles = "Auditor")]
        public async Task<IActionResult> GetRequestedProjectIds()
        {
            var ids = await _service.GetRequestedProjectIdsAsync();
            return Ok(ids);
        }

        [HttpGet("dashboard/{eventId}")]
        [Authorize(Roles = "Auditor")]
        public async Task<IActionResult> GetAuditDashboardByEvent(string eventId)
        {
            var requests = await _service.GetDashboardByEventAsync(eventId);
            var result = requests.Select(r => new
            {
                AuditId = r.AuditId,
                ProjectId = r.ProjectId,
                ProjectTitle = r.ProjectTitle,
                RequestedAt = r.RequestedAt
            });
            return Ok(result);
        }
    }
}
