using System;
using System.Collections.Generic;
using System.Text;

namespace Votify.Domain.EventFolder
{
    public class EventDashboardDto
    {
        public int TotalVotantes { get; set; } = 50;
        public int VotosEmitidos { get; set; }
        public List<ProjectResultDto> Ranking { get; set; } = new();
    }

    public class ProjectResultDto
    {
        public string Nombre { get; set; }
        public string Categoria { get; set; }
        public int Puntos { get; set; }
    }
}
