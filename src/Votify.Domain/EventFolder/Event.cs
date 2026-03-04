using System;
using System.Collections.Generic;
using System.Text;

namespace Votify.Domain.EventFolder;

public class Event
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public int MaxProjects { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    protected Event(string name, int maxProjects, DateTime startDate, string? description)
    {
        this.Id = Guid.NewGuid().ToString();
        this.Name = name;
        this.MaxProjects = maxProjects;
        this.StartDate = startDate;

        if(description is not null) {  this.Description = description; }
    }
}
