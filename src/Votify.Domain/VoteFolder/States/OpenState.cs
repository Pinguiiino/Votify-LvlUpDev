namespace Votify.Domain.VoteFolder.States;

public sealed class OpenState : IVotingSessionState
{
    public static readonly OpenState Instance = new();
    private OpenState() { }

    public string? StatusKey => "open";

    public void Abrir(VotingSession ctx)
        => throw new InvalidOperationException("La sesión ya está abierta.");

    public void Pausar(VotingSession ctx)
        => ctx.TransicionarA(PausedState.Instance);

    public void Reanudar(VotingSession ctx)
        => throw new InvalidOperationException("La sesión ya está abierta, no está pausada.");

    public void Cerrar(VotingSession ctx)
    {
        ctx.AdjustedCloseAt = DateTime.UtcNow;
        ctx.TransicionarA(ClosedState.Instance);
    }

    public bool PuedeVotar(VotingSession ctx)
    {
        var now = DateTime.UtcNow;
        if (ctx.OpenAt.HasValue && now < ctx.OpenAt.Value) return false;
        if (ctx.EffectiveCloseAt.HasValue && now > ctx.EffectiveCloseAt.Value) return false;
        return true;
    }
}
