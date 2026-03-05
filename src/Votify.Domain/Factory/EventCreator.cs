using Votify.Domain.EventFolder;
using System;

namespace Votify.Factory;


public abstract class EventCreator
{

    public abstract Event Create(string name, int maxProjects, DateTime startDate, string? description = null);


}