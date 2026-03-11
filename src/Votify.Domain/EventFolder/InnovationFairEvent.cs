namespace Votify.Domain.EventFolder;

/// <summary>
/// ConcreteProduct: evento tipo Feria de Innovación.
/// </summary>
public class InnovationFairEvent : Event
{
    public InnovationFairEvent() { }

    public InnovationFairEvent(string name, int maxProjects, DateTime startDate, string? description = null)
        : base(name, maxProjects, startDate, description) { }

    public override string Modality() => "Innovation Fair";
}