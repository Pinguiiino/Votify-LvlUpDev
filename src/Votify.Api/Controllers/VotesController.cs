using Microsoft.AspNetCore.Mvc;
using Votify.Domain.VoteFolder;

namespace Votify.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VotesController : ControllerBase
    {
        private readonly VoteService _voteService;

        public VotesController(VoteService voteService) => _voteService = voteService;

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] VoteRequest dto)
        {
            try
            {
                var result = await _voteService.CastVoteAsync(dto.ProjectId, dto.CategoryId, dto.EventId, dto.UserId);
                return Ok(new { id = result.Id, message = "Voto registrado" });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("by-user")]
        public async Task<IActionResult> GetByUser(string userId, string categoryId)
        {
            var votes = await _voteService.GetVotesByUserInCategoryAsync(userId, categoryId);
            return Ok(votes);
        }
    }

    public record VoteRequest(string ProjectId, string CategoryId, string EventId, string UserId);
}
