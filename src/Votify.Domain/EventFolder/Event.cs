using System;
using Votify.Domain.UserFolder;

namespace Votify.Domain.EventFolder;

public abstract class Event
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public int MaxProjects { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? ImageUrl { get; set; }
    public List<GeneralUser> Participants { get; set; }
    public List<GeneralUser> Public { get; set; }
    public string Organizer { get; set; } 
    public string Auditor { get; set; }

    protected Event() { }

    protected Event(string name, int maxProjects, DateTime startDate, DateTime endDate,
                    string? description = null, string? imageUrl = null)
    {
        this.Id = Guid.NewGuid().ToString();
        this.Name = name;
        this.MaxProjects = maxProjects;
        this.StartDate = startDate;
        this.EndDate = endDate;
        this.Description = description;
        this.ImageUrl = imageUrl;
    }

    public abstract string Modality();

    public virtual string Summary()
        => $"{this.Name} [{Modality()}] — hasta {this.MaxProjects} proyectos, {this.StartDate:d} → {this.EndDate:d}";
}
