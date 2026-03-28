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
    public int TopNProjectsAllowed { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public virtual List<VotingSession> VotingSessions { get; set; } = new();

    protected Event() { }

    protected Event(string name, int maxProjects, DateTime startDate, DateTime endDate, int topNProjectsAllowed,
                    string? description = null)
    {
        this.Id = Guid.NewGuid().ToString();
        this.Name = name;
        this.MaxProjects = maxProjects;
        this.StartDate = startDate;
        this.EndDate = endDate;
        this.TopNProjectsAllowed = topNProjectsAllowed;
        this.Description = description;
    }

    public abstract string Modality();

    public virtual string Summary()
        => $"{this.Name} [{Modality()}] — hasta {this.MaxProjects} proyectos, {this.StartDate:d} → {this.EndDate:d}";
}