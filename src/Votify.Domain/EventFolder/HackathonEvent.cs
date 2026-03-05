namespace Votify.Domain.EventFolder;


public class HackathonEvent : Event
{
    public HackathonEvent() { }

    public HackathonEvent(string name, int maxProjects, DateTime startDate, string? description = null)
        : base(name, maxProjects, startDate, description) { }

    public override string Modality() => "Hackathon";
}