namespace Votify.Domain.VoteFolder.States;

public interface IVotingSessionState
{
    string? StatusKey { get; }
    void Abrir(VotingSession ctx);
    void Pausar(VotingSession ctx);
    void Reanudar(VotingSession ctx);
    void Cerrar(VotingSession ctx);
    bool PuedeVotar(VotingSession ctx);
}
