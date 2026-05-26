namespace Votify.Domain.VoteFolder.States;

public sealed class PausedState : IVotingSessionState
{
    public static readonly PausedState Instance = new();
    private PausedState() { }

    public string? StatusKey => "paused";

    public void Abrir(VotingSession ctx)
        => throw new InvalidOperationException("La sesión está pausada. Use 'Reanudar' para reabrirla.");

    public void Pausar(VotingSession ctx)
        => throw new InvalidOperationException("La sesión ya está pausada.");

    public void Reanudar(VotingSession ctx)
        => ctx.TransicionarA(OpenState.Instance);

    public void Cerrar(VotingSession ctx)
    {
        ctx.AdjustedCloseAt = DateTime.UtcNow;
        ctx.TransicionarA(ClosedState.Instance);
    }

    public bool PuedeVotar(VotingSession ctx) => false;
}
