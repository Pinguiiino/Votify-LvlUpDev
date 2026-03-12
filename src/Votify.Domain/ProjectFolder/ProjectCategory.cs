using System;
using System.Collections.Generic;
using System.Text;
using Votify.Domain.CategoryFolder;

namespace Votify.Domain.ProjectFolder
{
    public class ProjectCategory
    {
        public Project? Project { get; set; }
        public string? ProjectId { get; set; }
        public Category? Category { get; set; }
        public string? CategoryId { get; set; }

        public ProjectCategory() { }

        public ProjectCategory(string projectId, string categoryId) {
            ProjectId = projectId;
            CategoryId = categoryId;
        }
    }
}
