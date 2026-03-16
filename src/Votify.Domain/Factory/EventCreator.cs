using System;
using Votify.Domain.EventFolder;

namespace Votify.Domain.Factory
{
    public abstract class EventCreator
    {
        public abstract Event Create(string name, int maxProjects,
                                     DateTime startDate, DateTime endDate,
                                     string? description = null);

        public string BuildSummary(string name, int maxProjects,
                                   DateTime startDate, DateTime endDate,
                                   string? description = null)
        {
            Event ev = Create(name, maxProjects, startDate, endDate, description);
            return ev.Summary();
        }
    }
}
