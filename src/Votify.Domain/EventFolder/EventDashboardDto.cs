using System;
using System.Collections.Generic;
using System.Text;

namespace Votify.Domain.EventFolder
{
    public class EventDashboardDto
    {
        public int TotalVotantes { get; set; }
        public int VotosEmitidos { get; set; }
        public List<ProjectResultDto> Ranking { get; set; } = new();
        public List<SessionProgressDto> SessionProgresses { get; set; } = new();
    }

    public class ProjectResultDto
    {
        public string Nombre { get; set; }
        public string Categoria { get; set; }
        public int Puntos { get; set; }
    }

    public class SessionProgressDto
    {
        public string VotingSessionId { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string SessionName { get; set; } = string.Empty;
        public int UniqueVoters { get; set; }
        public int TotalVoters { get; set; }
    }
}
