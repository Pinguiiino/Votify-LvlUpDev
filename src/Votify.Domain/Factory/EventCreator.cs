using Votify.Domain.EventFolder;
using System;
using Votify.Domain.CategoryFolder;

namespace Votify.Domain.Factory
{
    public abstract class EventCreator
    {
        public abstract Event Create(string name, int maxProjects, DateTime startDate, string modality, List<Category> associatedCategories, string? description = null);

        public string BuildSummary(string name, int maxProjects, DateTime startDate, string modality, List<Category> associatedCategories, string? description = null)
        {
            Event ev = Create(name, maxProjects, startDate, modality, associatedCategories, description);
            return ev.Summary();
        }
    }
}
