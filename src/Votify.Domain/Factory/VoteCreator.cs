using Votify.Domain.VoteFolder;

namespace Votify.Factory;

/// <summary>
/// Creator abstracto del patrón Factory Method para la familia Vote.
/// </summary>
public abstract class VoteCreator
{
    /// <summary>
    /// Factory Method: crea y devuelve un Vote concreto.
    /// </summary>
    public abstract Vote Create(string projectId, string userId, double rawScore);


}






