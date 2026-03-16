using System;
using System.Collections.Generic;
using Votify.Domain.VoteFolder;

namespace Votify.Domain.EventFolder;

public abstract class Event
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public int MaxProjects { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public virtual List<VotingSession> VotingSessions { get; set; } = new();

    protected Event() { }

    protected Event(string name, int maxProjects, DateTime startDate, DateTime endDate,
                    string? description = null)
    {
        Id = Guid.NewGuid().ToString();
        Name = name;
        MaxProjects = maxProjects;
        StartDate = startDate;
        EndDate = endDate;
        Description = description;
    }

    public abstract string Modality();

    public virtual string Summary()
        => $"{Name} [{Modality()}] — hasta {MaxProjects} proyectos, {StartDate:d} → {EndDate:d}";
}