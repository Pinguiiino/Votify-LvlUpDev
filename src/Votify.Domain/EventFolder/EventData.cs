using System;
using System.Collections.Generic;
using System.Text;

namespace Votify.Domain.EventFolder
{
    public class EventData
    {
        public string Name { get; set; } = string.Empty;
        public string Modality { get; set; } = string.Empty;
        public int MaxProjects { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string AuditorEmail { get; set; } = string.Empty;
    }
}
