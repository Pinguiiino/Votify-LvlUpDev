using System;
using System.Collections.Generic;
using System.Text;

namespace Votify.Domain.EventFolder
{
    public class ModalityEvent : Event
    {
        public string Modality { get; set; }
        public ModalityEvent() { }

        public ModalityEvent(string name, int maxProjects, DateTime startDate, string modality, string? description = null) 
            : base(name, maxProjects, startDate, description)
        { 
            this.Modality = modality;
        }
    }
}
