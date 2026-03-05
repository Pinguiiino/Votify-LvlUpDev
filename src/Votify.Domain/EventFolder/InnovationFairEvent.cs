namespace Votify.Domain.EventFolder;


public class InnovationFairEvent : Event
{
    public InnovationFairEvent() { }

    public InnovationFairEvent(string name, int maxProjects, DateTime startDate, string? description = null)
        : base(name, maxProjects, startDate, description) { }

    public override string Modality() => "Innovation Fair";
}