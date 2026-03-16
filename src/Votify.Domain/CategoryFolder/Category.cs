using System;
using System.Collections.Generic;
using System.Linq;
using Votify.Domain.ProjectFolder;

namespace Votify.Domain.CategoryFolder;

public enum VotingMode
{
    Scored,
    PointPool,
    TopN,
    Ranked
}

public class Category
{
    public string Id { get; set; }
    public string EventId { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public VotingMode VotingMode { get; set; } = VotingMode.Scored;

    /// PointPool: puntos a repartir por votante.
    /// TopN: proyectos que puede elegir cada votante.
    /// Ranked: puntos asignados al 1er proyecto elegido.


    public int? VotingParameter { get; set; }

    public bool AllowSelfVoting { get; set; } = false; //si un participante puede votar en su propia categoría

    public virtual List<Criterion> Criteria { get; set; } = new();
    public virtual List<Prize> Prizes { get; set; } = new();
    public virtual List<ProjectCategory> ProjectCategories { get; set; } = new();

    public Category() { }

    public Category(string eventId, string name, VotingMode votingMode = VotingMode.Scored,
                    string? description = null, bool allowSelfVoting = false)
    {
        Id = Guid.NewGuid().ToString();
        EventId = eventId;
        Name = name;
        VotingMode = votingMode;
        Description = description;
        AllowSelfVoting = allowSelfVoting;
    }

    public bool WeightsAreValid()
        => VotingMode != VotingMode.Scored ||
           Math.Abs(Criteria.Sum(c => c.Weight) - 1.0) < 0.001;
}