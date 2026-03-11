namespace Votify.Domain.EventFolder;

/// <summary>
/// ConcreteProduct: evento tipo Hackathon.
/// </summary>
public class HackathonEvent : Event
{
    public HackathonEvent() { }

    public HackathonEvent(string name, int maxProjects, DateTime startDate, string? description = null)
        : base(name, maxProjects, startDate, description) { }

    public override string Modality() => "Hackathon";
}
