using Votify.Domain.EventFolder;
using System;

namespace Votify.Factory;

public class HackathonEventCreator : EventCreator
{
    public override Event Create(string name, int maxProjects, DateTime startDate, string? description = null)
        => new HackathonEvent(name, maxProjects, startDate, description);
}