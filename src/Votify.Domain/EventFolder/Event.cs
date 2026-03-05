using System;
using System.Collections.Generic;
using System.Text;

namespace Votify.Domain.EventFolder;

public abstract class Event
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public int MaxProjects { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public Event() { }
    public Event(string name, int maxProjects, DateTime startDate, string? description = null)
    {
        this.Id = Guid.NewGuid().ToString();
        this.Name = name;
        this.MaxProjects = maxProjects;
        this.StartDate = startDate;
        this.Description = description;

        if (description is not null) { this.Description = description; }
    }
    public abstract string Modality();
}
