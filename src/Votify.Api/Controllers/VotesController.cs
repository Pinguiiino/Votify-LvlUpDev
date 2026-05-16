using Microsoft.AspNetCore.Mvc;
using Votify.Domain.VoteFolder;
using Votify.Domain.VoteFolder.Strategies;

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

        [HttpPost("cast")]
        public async Task<IActionResult> Cast([FromBody] CastVoteRequest dto)
        {
            try
            {
                var input = dto.ToStrategyInput();
                await _service.CastVotesByStrategyAsync(dto.VotingSessionId, input);
                return Ok(new { message = "Voto registrado correctamente." });
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPost("batch")]
        public async Task<IActionResult> CastTopNVotes([FromBody] BatchVoteRequest dto)
        {
            try
            {
                var input = new VoteStrategyInput
                {
                    UserId = dto.UserId,
                    CategoryId = dto.CategoryId,
                    RankedProjects = dto.RankedProjects
                        .Select(r => new RankedProjectInput(r.ProjectId, r.Position, r.Comment))
                        .ToArray()
                };
                await _service.CastVotesByStrategyAsync(dto.VotingSessionId, input);
                return Ok(new { message = "Votos registrados correctamente." });
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPost("weighted-batch")]
        public async Task<IActionResult> CastWeightedVotes([FromBody] WeightedBatchRequest dto)
        {
            try
            {
                var input = new VoteStrategyInput
                {
                    UserId = dto.UserId,
                    CategoryId = dto.CategoryId,
                    WeightedProjects = dto.Scores
                        .Select(s => new WeightedProjectInput(
                            s.ProjectId,
                            s.Comment,
                            s.CriterionScores
                                .Select(cs => new CriterionScoreInput(cs.CriterionId, cs.Score, cs.Comment))
                                .ToArray()))
                        .ToArray()
                };
                await _service.CastVotesByStrategyAsync(dto.VotingSessionId, input);
                return Ok(new { message = "Evaluación registrada." });
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPost("point-distribution")]
        public async Task<IActionResult> CastPointDistribution([FromBody] PointDistributionRequest dto)
        {
            try
            {
                var input = new VoteStrategyInput
                {
                    UserId = dto.UserId,
                    CategoryId = dto.CategoryId,
                    PointAllocations = dto.Allocations
                        .Select(a => new PointAllocationInput(a.ProjectId, a.Points, a.Comment))
                        .ToArray()
                };
                await _service.CastVotesByStrategyAsync(dto.VotingSessionId, input);
                return Ok(new { message = "Reparto de puntos registrado." });
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpGet("by-user")]
        public async Task<IActionResult> GetByUser(string userId, string categoryId, string? votingSessionId = null)
        {
            var votes = await _service.GetUserVotesAsync(userId, categoryId);
            if (!string.IsNullOrEmpty(votingSessionId))
                votes = votes.Where(v => v.VotingSessionId == votingSessionId).ToList();
            return Ok(votes.Select(v => new { v.VotedProjectId, v.TopPosition, v.Comment }));
        }

        [HttpGet("comments/{projectId}")]
        public async Task<IActionResult> GetCommentsByProject(string projectId)
        {
            var comments = await _service.GetCommentsByProjectAsync(projectId);
            return Ok(comments.Select(c => new { c.Comment, c.TopPosition, c.VoteType, c.EvaluationType }));
        }

        [HttpGet("weighted")]
        public async Task<IActionResult> GetWeightedByUser(string userId, string votingSessionId)
        {
            var votes = await _service.GetWeightedVotesByUserAndSessionAsync(userId, votingSessionId);
            return Ok(votes);
        }

        [HttpGet("point-distribution")]
        public async Task<IActionResult> GetPointDistributionByUser(
        string userId, string categoryId, string votingSessionId)
        {
            var votes = await _service.GetPointDistributionVotesByUserAsync(
                userId, categoryId, votingSessionId);
            return Ok(votes);
        }
    }

    public sealed class CastVoteRequest
    {
        public string UserId { get; init; } = "";
        public string CategoryId { get; init; } = "";
        public string VotingSessionId { get; init; } = "";

        public List<RankedProjectDto>? RankedProjects { get; init; }
        public List<WeightedProjectScoreDto>? WeightedProjects { get; init; }
        public List<PointAllocationDto>? PointAllocations { get; init; }

        internal VoteStrategyInput ToStrategyInput() => new()
        {
            UserId = UserId,
            CategoryId = CategoryId,
            RankedProjects = RankedProjects?
                .Select(r => new RankedProjectInput(r.ProjectId, r.Position, r.Comment))
                .ToArray() ?? Array.Empty<RankedProjectInput>(),
            WeightedProjects = WeightedProjects?
                .Select(s => new WeightedProjectInput(
                    s.ProjectId, s.Comment,
                    s.CriterionScores.Select(cs => new CriterionScoreInput(cs.CriterionId, cs.Score, cs.Comment)).ToArray()))
                .ToArray() ?? Array.Empty<WeightedProjectInput>(),
            PointAllocations = PointAllocations?
                .Select(a => new PointAllocationInput(a.ProjectId, a.Points, a.Comment))
                .ToArray() ?? Array.Empty<PointAllocationInput>()
        };
    }

    public record RankedProjectDto(string ProjectId, int Position, string? Comment);
    public record BatchVoteRequest(string CategoryId, string EventId, string UserId, string VotingSessionId, List<RankedProjectDto> RankedProjects);

    public record WeightedCriterionScoreDto(string CriterionId, double Score, string? Comment = null);
    public record WeightedProjectScoreDto(string ProjectId, string? Comment, List<WeightedCriterionScoreDto> CriterionScores);
    public record WeightedBatchRequest(string UserId, string CategoryId, string VotingSessionId, List<WeightedProjectScoreDto> Scores);

    public record PointAllocationDto(string ProjectId, int Points, string? Comment);
    public record PointDistributionRequest(string UserId, string CategoryId, string VotingSessionId, List<PointAllocationDto> Allocations);
}
