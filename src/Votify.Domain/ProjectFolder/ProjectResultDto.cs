using System;
using System.Collections.Generic;
using System.Text;

namespace Votify.Domain.ProjectFolder;

public class ProjectResultsDto
{
    public string ProjectId { get; set; } = string.Empty;
    public string ProjectTitle { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public DateTime EventEndDate { get; set; }

    /// <summary>Resultado en cada categoría en la que participa el proyecto.</summary>
    public List<CategoryResultDto> CategoryResults { get; set; } = new();

    /// <summary>Indica si el participante puede ya generar el certificado.</summary>
    public bool CanGenerateCertificate { get; set; }
}

public class CategoryResultDto
{
    public string CategoryId { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;

    public SessionResultDto? JurySession { get; set; }
    public SessionResultDto? PublicSession { get; set; }
}

public class SessionResultDto
{
    public string SessionId { get; set; } = string.Empty;
    public string SessionName { get; set; } = string.Empty;
    public bool IsClosed { get; set; }

    /// <summary>1-indexed. Null si la sesión está cerrada pero no hay votos para este proyecto.</summary>
    public int? Position { get; set; }

    /// <summary>Número total de proyectos rankeados en esta sesión.</summary>
    public int? TotalRanked { get; set; }

    /// <summary>Puntos obtenidos por el proyecto.</summary>
    public int Points { get; set; }
}