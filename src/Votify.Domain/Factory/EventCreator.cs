using Votify.Domain.EventFolder;
using System;

namespace Votify.Factory;

/// <summary>
/// Creator abstracto del patrón Factory Method para la familia Event.
/// Declara el factory method Create() que las subclases deben implementar.
/// El cliente programa contra esta abstracción, nunca contra los tipos concretos.
/// </summary>
public abstract class EventCreator
{
    /// <summary>
    /// Factory Method: crea y devuelve un Event concreto.
    /// Las subclases deciden qué tipo instanciar.
    /// </summary>
    public abstract Event Create(string name, int maxProjects, DateTime startDate, string? description = null);

    /// <summary>
    /// Operación de ejemplo que usa el producto sin conocer su tipo exacto.
    /// </summary>
    public string BuildSummary(string name, int maxProjects, DateTime startDate, string? description = null)
    {
        Event ev = Create(name, maxProjects, startDate, description);
        return ev.Summary();
    }
}
