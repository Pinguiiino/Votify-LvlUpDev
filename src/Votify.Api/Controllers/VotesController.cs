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
                // Borrar votos anteriores del usuario en esta categoría
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
                        comment: string.IsNullOrWhiteSpace(rank.Comment) ? null : rank.Comment.Trim()
                    );
                    nuevoVoto.IntegrityHash = Guid.NewGuid().ToString();
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

        [HttpGet("comments/{projectId}")]
        public async Task<IActionResult> GetCommentsByProject(string projectId)
        {
            // ToListAsync() primero: EF Core no puede traducir 'v is ExpertVote' a SQL
            var votes = await _context.Votes
                .Where(v => v.VotedProjectId == projectId && v.Comment != null && v.Comment != "")
                .OrderBy(v => v.TopPosition)
                .ToListAsync();

            var comments = votes.Select(v => new
            {
                v.Comment,
                v.TopPosition,
                VoteType = v is ExpertVote ? "Expert" : "Public"
            });

            return Ok(comments);
        }
    }


    public record RankedProjectDto(string ProjectId, int Position, string? Comment);
    public record BatchVoteRequest(string CategoryId, string EventId, string UserId, string VotingSessionId, List<RankedProjectDto> RankedProjects);
}