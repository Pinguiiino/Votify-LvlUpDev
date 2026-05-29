using Votify.Domain.CategoryFolder;
using Votify.Domain.Factory;
using Votify.Domain.ProjectFolder;
using Votify.Domain.VoteFolder;
using Votify.Domain.VoteFolder.Strategies;

namespace Votify.Domain.Facade;

public class VotingFacade
{
    private readonly IVotingSessionRepository _sessionRepo;
    private readonly IVoteRepository _voteRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly IProjectRepository _projectRepo;
    private readonly VotingStrategyResolver _strategyResolver;
    private readonly VoteCreatorFactory _voteCreatorFactory;

    public VotingFacade(
        IVotingSessionRepository sessionRepo,
        IVoteRepository voteRepo,
        ICategoryRepository categoryRepo,
        IProjectRepository projectRepo,
        VotingStrategyResolver strategyResolver,
        VoteCreatorFactory voteCreatorFactory)
    {
        _sessionRepo = sessionRepo;
        _voteRepo = voteRepo;
        _categoryRepo = categoryRepo;
        _projectRepo = projectRepo;
        _strategyResolver = strategyResolver;
        _voteCreatorFactory = voteCreatorFactory;
    }

    
    public async Task<VoteResultDto> SubmitVoteAsync(VoteRequestDto request)
    {
        var session = await ValidateSessionAsync(request.VotingSessionId, request.CategoryId);
        await ValidateSelfVotingAsync(request);

        var strategy = _strategyResolver.Resolve(session.EvaluationType);
        var input = BuildStrategyInput(request);
        await strategy.ValidateAsync(session, input);
        await strategy.ExecuteAsync(session, input);

        return new VoteResultDto(
            Success: true,
            Message: "Voto registrado correctamente.",
            SessionId: session.Id,
            EvaluationType: session.EvaluationType.ToString());
    }

    
    private async Task<VotingSession> ValidateSessionAsync(string sessionId, string categoryId)
    {
        var session = await _sessionRepo.GetByIdAsync(sessionId)
            ?? throw new InvalidOperationException("La sesión de votación no existe.");

        if (session.CategoryId != categoryId)
            throw new InvalidOperationException("La sesión no pertenece a la categoría indicada.");

        if (!session.IsOpen)
            throw new InvalidOperationException("La sesión de votación no está abierta.");

        return session;
    }

    
    private async Task ValidateSelfVotingAsync(VoteRequestDto request)
    {
        var category = await _categoryRepo.GetByIdAsync(request.CategoryId);
        if (category == null || category.AllowSelfVoting)
            return;

        var projectIds = ExtractProjectIds(request);
        foreach (var projectId in projectIds)
        {
            var project = await _projectRepo.GetByIdAsync(projectId);
            if (project != null && project.OwnerId == request.UserId)
                throw new InvalidOperationException(
                    "No puedes votar tu propio proyecto en esta categoría.");
        }
    }

    private static List<string> ExtractProjectIds(VoteRequestDto request)
    {
        var ids = new List<string>();
        if (request.RankedProjects != null)
            ids.AddRange(request.RankedProjects.Select(r => r.ProjectId));
        if (request.WeightedProjects != null)
            ids.AddRange(request.WeightedProjects.Select(w => w.ProjectId));
        if (request.PointAllocations != null)
            ids.AddRange(request.PointAllocations.Where(p => p.Points > 0).Select(p => p.ProjectId));
        return ids.Distinct().ToList();
    }

    private static VoteStrategyInput BuildStrategyInput(VoteRequestDto request) => new()
    {
        UserId = request.UserId,
        CategoryId = request.CategoryId,
        RankedProjects = request.RankedProjects?
            .Select(r => new RankedProjectInput(r.ProjectId, r.Position, r.Comment))
            .ToArray() ?? Array.Empty<RankedProjectInput>(),
        WeightedProjects = request.WeightedProjects?
            .Select(w => new WeightedProjectInput(
                w.ProjectId, w.Comment,
                w.CriterionScores.Select(cs => new CriterionScoreInput(cs.CriterionId, cs.Score, cs.Comment)).ToArray()))
            .ToArray() ?? Array.Empty<WeightedProjectInput>(),
        PointAllocations = request.PointAllocations?
            .Select(p => new PointAllocationInput(p.ProjectId, p.Points, p.Comment))
            .ToArray() ?? Array.Empty<PointAllocationInput>()
    };
}

public record VoteRequestDto(
    string UserId,
    string CategoryId,
    string VotingSessionId,
    List<RankedProjectDto>? RankedProjects = null,
    List<WeightedProjectDto>? WeightedProjects = null,
    List<PointAllocationDto>? PointAllocations = null);

public record RankedProjectDto(string ProjectId, int Position, string? Comment);
public record WeightedProjectDto(string ProjectId, string? Comment, List<CriterionScoreDto> CriterionScores);
public record CriterionScoreDto(string CriterionId, double Score, string? Comment);
public record PointAllocationDto(string ProjectId, int Points, string? Comment);

public record VoteResultDto(
    bool Success,
    string Message,
    string SessionId,
    string EvaluationType);