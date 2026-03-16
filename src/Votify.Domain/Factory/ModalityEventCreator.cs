using System;
using Votify.Domain.EventFolder;

namespace Votify.Domain.Factory
{
    public class ModalityEventCreator : EventCreator
    {
        private readonly string _modality;

        public ModalityEventCreator(string modality)
        {
            _modality = modality;
        }

        public override Event Create(string name, int maxProjects,
                                     DateTime startDate, DateTime endDate,
                                     string? description = null)
            => new ModalityEvent(name, maxProjects, startDate, endDate, _modality, description);
    }
}

