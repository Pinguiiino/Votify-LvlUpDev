using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Votify.Domain;
using Votify.Domain.UserFolder;
using Votify.Domain.VoteFoler;

namespace Votify.Infrastructure;

public class VotifyDbContext : DbContext
{
    public VotifyDbContext(DbContextOptions<VotifyDbContext> options) : base(options) { }

    public DbSet<Vote> Votes { get; set; }
    public DbSet<User> Usuarios { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Vote>()
            .HasDiscriminator<string>("VoteType")
            .HasValue<ExpertVote>("Expert")
            .HasValue<PublicVote>("Public");

        modelBuilder.Entity<User>()
            .HasDiscriminator<string>("TipoUsuario") 
            .HasValue<Organizer>("Organizador")
            .HasValue<Public>("PublicoGeneral")
            .HasValue<Jury>("Jurado")
            .HasValue<Participant>("Participante");
    }
}
