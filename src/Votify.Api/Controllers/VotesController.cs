using Microsoft.AspNetCore.Mvc;
using Votify.Domain.VoteFolder;

namespace Votify.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VotesController : ControllerBase
    {
        private readonly VoteService _service;

        public VotesController(VoteService service)
        {
            _service = service;
        }

        [HttpPost("batch")]
        public async Task<IActionResult> CastTopNVotes([FromBody] BatchVoteRequest dto)
        {
            try
            {
                var ranked = dto.RankedProjects
                    .Select(r => (r.ProjectId, r.Position, r.Comment))
                    .ToList();

                await _service.CastTopNVotesAsync(dto.UserId, dto.CategoryId, dto.VotingSessionId, ranked);

                return Ok(new { message = "Votos registrados correctamente" });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("by-user")]
        public async Task<IActionResult> GetByUser(string userId, string categoryId)
        {
            var votes = await _service.GetUserVotesAsync(userId, categoryId);
            return Ok(votes.Select(v => new { v.VotedProjectId, v.TopPosition }));
        }

        [HttpGet("comments/{projectId}")]
        public async Task<IActionResult> GetCommentsByProject(string projectId)
        {
            var comments = await _service.GetCommentsByProjectAsync(projectId);
            return Ok(comments.Select(c => new { c.Comment, c.TopPosition, c.VoteType }));
        }
    }

    public record RankedProjectDto(string ProjectId, int Position, string? Comment);
    public record BatchVoteRequest(string CategoryId, string EventId, string UserId, string VotingSessionId, List<RankedProjectDto> RankedProjects);
}