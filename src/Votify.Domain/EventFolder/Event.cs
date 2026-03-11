using System;

namespace Votify.Domain.EventFolder;

/// <summary>
/// Product abstracto del patrón Factory Method para la familia Event.
/// Define el contrato común de todos los tipos de evento.
/// </summary>
public abstract class Event
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public int MaxProjects { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    protected Event() { }

    protected Event(string name, int maxProjects, DateTime startDate, string? description = null)
    {
        this.Id = Guid.NewGuid().ToString();
        this.Name = name;
        this.MaxProjects = maxProjects;
        this.StartDate = startDate;
        this.Description = description;
    }

    /// <summary>
    /// Factory Method hook: devuelve la modalidad concreta del evento.
    /// Cada subclase lo sobreescribe con su propio valor.
    /// </summary>
    public abstract string Modality();

    public virtual string Summary()
        => $"[{Modality()}] {Name} — hasta {MaxProjects} proyectos, desde {StartDate:d}";
}
