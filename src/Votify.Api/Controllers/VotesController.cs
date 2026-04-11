using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Votify.Domain.Factory;
using Votify.Domain.VoteFolder;
using Votify.Infrastructure;

namespace Votify.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VotesController : ControllerBase
    {
        private readonly VotifyDbContext _context;

        public VotesController(VotifyDbContext context)
        {
            _context = context;
        }

        [HttpPost("batch")]
        public async Task<IActionResult> CastTopNVotes([FromBody] BatchVoteRequest dto)
        {
            try
            {
                // Eliminar votos anteriores del mismo usuario en esta categoría
                var votosAnteriores = await _context.Votes
                    .Where(v => v.UserId == dto.UserId && v.CategoryId == dto.CategoryId)
                    .ToListAsync();

                if (votosAnteriores.Any())
                    _context.Votes.RemoveRange(votosAnteriores);

                VoteCreator voteCreator = new PublicVoteCreator();

                foreach (var rank in dto.RankedProjects)
                {
                    var nuevoVoto = voteCreator.Create(
                        votingSessionId: dto.VotingSessionId,
                        projectId: rank.ProjectId,
                        userId: dto.UserId,
                        categoryId: dto.CategoryId,
                        topPosition: rank.Position,
                        comment: rank.Comment   // ← persiste el comentario
                    );

                    _context.Votes.Add(nuevoVoto);
                }

                await _context.SaveChangesAsync();
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
            var votes = await _context.Votes
                .Where(v => v.UserId == userId && v.CategoryId == categoryId)
                .OrderBy(v => v.TopPosition)
                .Select(v => new { v.VotedProjectId, v.TopPosition })
                .ToListAsync();

            return Ok(votes);
        }
    }

    public record BatchVoteRequest(
        string CategoryId,
        string EventId,
        string UserId,
        string VotingSessionId,
        List<RankedProjectDto> RankedProjects);

    // Comment es opcional, si el votante no escribe nada se envía null
    public record RankedProjectDto(
        string ProjectId,
        int Position,
        string? Comment = null);
}
