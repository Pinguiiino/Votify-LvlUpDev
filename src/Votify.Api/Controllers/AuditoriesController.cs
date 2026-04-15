using Microsoft.AspNetCore.Mvc;
using Votify.Domain.VoteFolder;

namespace Votify.Api.Controllers
{ 
    [ApiController]
    [Route("api/[controller]")]
public class AuditController : ControllerBase
{
    private readonly IVoteRepository _voteRepository;

    public AuditController(IVoteRepository voteRepository) => _voteRepository = voteRepository;

    [HttpGet("project/{projectId}")]
    public async Task<IActionResult> GetProjectAudit(string projectId)
    {
        var votes = await _voteRepository.GetByProjectAsync(projectId);

        
        var auditTrail = votes.Select(v => new {
            v.Id,
            Voter = v.UserId,
            Date = v.CreatedAt,
            Hash = v.IntegrityHash
        });

        return Ok(auditTrail);
        }
    }
}
