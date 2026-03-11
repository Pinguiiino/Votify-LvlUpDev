using Votify.Domain.EventFolder;
using System;

namespace Votify.Domain.Factory
{
    public abstract class EventCreator
    {
        public abstract Event Create(string name, int maxProjects, DateTime startDate, string modality, string? description = null);

        public string BuildSummary(string name, int maxProjects, DateTime startDate, string modality, string? description = null)
        {
            Event ev = Create(name, maxProjects, startDate, modality, description);
            return ev.Summary();
        }
    }
}
