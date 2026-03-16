using System;
using System.Collections.Generic;
using System.Text;
using Votify.Domain.CategoryFolder;

namespace Votify.Domain.EventFolder
{
    public class ModalityEvent : Event
    {
        public string modality { get; set; }
        public ModalityEvent() { }

        public ModalityEvent(string name, int maxProjects, DateTime startDate, DateTime endDate, string modality, string? description = null)
            : base(name, maxProjects, startDate, endDate, description)
        {
            this.modality = modality;
        }

        public override string Modality() => modality;
    }
}
