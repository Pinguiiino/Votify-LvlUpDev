using System;
using System.Collections.Generic;
using System.Text;
using Votify.Domain.CategoryFolder;

namespace Votify.Domain.EventFolder
{
    public class ModalityEvent : Event
    {
        public string Modality { get; set; }
        public ModalityEvent() { }

        public ModalityEvent(string name, int maxProjects, DateTime startDate, string modality, List<Category> associatedCategories, string? description = null) 
            : base(name, maxProjects, startDate, associatedCategories, description)
        { 
            this.Modality = modality;
        }
    }
}
