namespace Votify.Domain.VoteFolder.Strategies;

public sealed class VoteStrategyInput
{
    public string UserId       { get; init; } = "";
    public string CategoryId   { get; init; } = "";

    public IReadOnlyList<RankedProjectInput> RankedProjects { get; init; }
        = Array.Empty<RankedProjectInput>();

    public IReadOnlyList<WeightedProjectInput> WeightedProjects { get; init; }
        = Array.Empty<WeightedProjectInput>();

    public IReadOnlyList<PointAllocationInput> PointAllocations { get; init; }
        = Array.Empty<PointAllocationInput>();
}

public sealed record RankedProjectInput(string ProjectId, int Position, string? Comment);

public sealed record WeightedProjectInput(
    string ProjectId,
    string? Comment,
    IReadOnlyList<CriterionScoreInput> CriterionScores);

public sealed record CriterionScoreInput(string CriterionId, double Score, string? Comment = null);

public sealed record PointAllocationInput(string ProjectId, int Points, string? Comment);
