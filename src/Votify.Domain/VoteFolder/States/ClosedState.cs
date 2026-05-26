namespace Votify.Domain.VoteFolder.States;

public sealed class ClosedState : IVotingSessionState
{
    public static readonly ClosedState Instance = new();
    private ClosedState() { }

    public string? StatusKey => "closed";

    public void Abrir(VotingSession ctx)
        => throw new InvalidOperationException("Una sesión cerrada no puede reabrirse.");

    public void Pausar(VotingSession ctx)
        => throw new InvalidOperationException("Una sesión cerrada no puede pausarse.");

    public void Reanudar(VotingSession ctx)
        => throw new InvalidOperationException("Una sesión cerrada no puede reanudarse.");

    public void Cerrar(VotingSession ctx)
        => throw new InvalidOperationException("La sesión ya está cerrada.");

    public bool PuedeVotar(VotingSession ctx) => false;
}
