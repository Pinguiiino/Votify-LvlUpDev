using Votify.Domain.EventFolder;
using System;

namespace Votify.Factory;

/// <summary>
/// ConcreteCreator: fabrica eventos de tipo Feria de Innovación.
/// </summary>
public class InnovationFairEventCreator : EventCreator
{
    public override Event Create(string name, int maxProjects, DateTime startDate, string? description = null)
        => new InnovationFairEvent(name, maxProjects, startDate, description);
}
