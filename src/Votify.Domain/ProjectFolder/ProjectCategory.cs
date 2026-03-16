using System.Collections.Generic;
using System.Linq;
using Votify.Domain.CategoryFolder;

namespace Votify.Domain.ProjectFolder;

public class ProjectCategory
{
    public string Id { get; set; }
    public string? ProjectId { get; set; }
    public string? CategoryId { get; set; }
    public int? FinalRank { get; set; }
    public bool IsRankManual { get; set; } = false;

    public virtual Project? Project { get; set; }
    public virtual Category? Category { get; set; }
    public virtual List<CriterionScore> CriterionScores { get; set; } = new();

    public ProjectCategory() { }

    public ProjectCategory(string projectId, string categoryId)
    {
        Id = Guid.NewGuid().ToString();
        ProjectId = projectId;
        CategoryId = categoryId;
    }

    public double WeightedScore()
        => CriterionScores
            .Where(cs => cs.Score.HasValue)
            .Sum(cs => cs.Score!.Value * (cs.Criterion?.Weight ?? 0));
}