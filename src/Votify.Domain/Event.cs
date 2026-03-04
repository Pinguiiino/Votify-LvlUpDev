using System;
using System.Collections.Generic;
using System.Text;

namespace Votify.Domain;

public abstract class Event
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MaxProjects { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    protected Event(string name, int maxProjects)
    {
        Name = name;
        MaxProjects = maxProjects;
    }

    public abstract string Modality();
}
