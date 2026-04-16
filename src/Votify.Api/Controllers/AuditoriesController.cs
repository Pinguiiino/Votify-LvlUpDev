using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Votify.Domain.AuditFolder;
using Votify.Domain.VoteFolder;
using Votify.Infrastructure;

namespace Votify.Api.Controllers
{
    [ApiController]
    [Route("api/audit")]
    public class AuditController : ControllerBase
    {
        private readonly IVoteRepository _voteRepository;
        private readonly VotifyDbContext _context;

        public AuditController(IVoteRepository voteRepository, VotifyDbContext context)
        {
            _voteRepository = voteRepository;
            _context = context;
        }


        [HttpGet("project/{projectId}")]
        public async Task<IActionResult> GetProjectAudit(string projectId)
        {
            var votes = await _voteRepository.GetByProjectAsync(projectId);

            var auditTrail = votes.Select(v => new
            {
                v.Id,
                Voter = v.UserId,
                Date = v.CreatedAt,
                Hash = v.IntegrityHash
            });

            return Ok(auditTrail);
        }


        [HttpPost("request/{projectId}")]
        public async Task<IActionResult> RequestAudit(string projectId)
        {

            bool exists = await _context.AuditRequests
                .AnyAsync(a => a.ProjectId == projectId);

            if (!exists)
            {
                _context.AuditRequests.Add(new AuditRequest(projectId));
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Solicitud registrada." });
        }


        [HttpGet("requested")]
        public async Task<IActionResult> GetRequestedProjectIds()
        {
            var ids = await _context.AuditRequests
                .Select(a => a.ProjectId)
                .ToListAsync();

            return Ok(ids);
        }
    }
}
