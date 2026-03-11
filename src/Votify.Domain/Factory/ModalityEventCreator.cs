using System;
using System.Collections.Generic;
using System.Text;
using Votify.Domain.EventFolder;
using Votify.Factory;

namespace Votify.Domain.Factory
{
    public class ModalityEventCreator : EventCreator
    {
        public override Event Create(string name, int maxProjects, DateTime startDate, string modality, string? description = null)
        => new ModalityEvent(name, maxProjects, startDate, modality, description);
    }
}
