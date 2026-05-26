namespace Votify.Domain.VoteFolder.States;

public sealed class ScheduledState : IVotingSessionState
{
    public static readonly ScheduledState Instance = new();
    private ScheduledState() { }

    public string? StatusKey => null;

    public void Abrir(VotingSession ctx)
    {
        if (ctx.OpenAt.HasValue && ctx.OpenAt.Value > DateTime.UtcNow)
            ctx.OpenAt = DateTime.UtcNow;
        ctx.TransicionarA(OpenState.Instance);
    }

    public void Pausar(VotingSession ctx)
        => throw new InvalidOperationException("No se puede pausar una sesión que no está abierta.");

    public void Reanudar(VotingSession ctx)
        => throw new InvalidOperationException("No se puede reanudar una sesión que no está pausada.");

    public void Cerrar(VotingSession ctx)
        => throw new InvalidOperationException("No se puede cerrar una sesión que no ha sido abierta.");

    public bool PuedeVotar(VotingSession ctx)
    {
        var now = DateTime.UtcNow;
        return ctx.OpenAt.HasValue && ctx.CloseAt.HasValue
            && now >= ctx.OpenAt && now <= ctx.EffectiveCloseAt;
    }
}
