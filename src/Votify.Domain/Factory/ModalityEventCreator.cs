using System;
using Votify.Domain.EventFolder;

namespace Votify.Domain.Factory
{
    public class ModalityEventCreator : EventCreator
    {
        private readonly string Modality;

        public ModalityEventCreator(string modality)
        {
            this.Modality = modality;
        }

        public override Event Create(string name, int maxProjects,
                                     DateTime startDate, DateTime endDate,
                                     int topNProjectsAllowed, string? description = null)
            => new ModalityEvent(name, maxProjects, startDate, endDate, this.Modality, topNProjectsAllowed, description);
    }
}

