using System;

namespace Votify.Domain.ProjectFolder;

public class CriterionScore
{
    public string Id { get; set; }
    public string ProjectCategoryId { get; set; }
    public string CriterionId { get; set; }
    public double? Score { get; set; }
    public string? Comment { get; set; }

    public virtual ProjectCategory? ProjectCategory { get; set; }
    public virtual Votify.Domain.CategoryFolder.Criterion? Criterion { get; set; }

    public CriterionScore() { }

    public CriterionScore(string projectCategoryId, string criterionId,
                          double? score = null, string? comment = null)
    {
        Id = Guid.NewGuid().ToString();
        ProjectCategoryId = projectCategoryId;
        CriterionId = criterionId;
        Score = score;
        Comment = comment;
    }
}
