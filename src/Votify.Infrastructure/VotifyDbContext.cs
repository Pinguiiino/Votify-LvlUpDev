using Microsoft.EntityFrameworkCore;
using Votify.Domain.CategoryFolder;
using Votify.Domain.EventFolder;
using Votify.Domain.ProjectFolder;
using Votify.Domain.UserFolder;
using Votify.Domain.VoteFolder;

namespace Votify.Infrastructure;

public class VotifyDbContext : DbContext
{
    public VotifyDbContext(DbContextOptions<VotifyDbContext> options) : base(options) { }

    public DbSet<Vote> Votes { get; set; }
    public DbSet<User> Usuarios { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Project> Projects { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── 1. CONECTAR PADRES E HIJOS EXPLÍCITAMENTE (El parche para el error) ──
        modelBuilder.Entity<ModalityEvent>().HasBaseType<Event>();

        modelBuilder.Entity<AiProject>().HasBaseType<Project>();
        modelBuilder.Entity<SustainabilityProject>().HasBaseType<Project>();

        modelBuilder.Entity<ExpertVote>().HasBaseType<Vote>();
        modelBuilder.Entity<PublicVote>().HasBaseType<Vote>();

        modelBuilder.Entity<Organizer>().HasBaseType<User>();
        modelBuilder.Entity<Public>().HasBaseType<User>();
        modelBuilder.Entity<Jury>().HasBaseType<User>();
        modelBuilder.Entity<Participant>().HasBaseType<User>();

        // ── 2. DISCRIMINADORES (Table-Per-Hierarchy) ─────────────────────────
        modelBuilder.Entity<Event>()
            .HasDiscriminator<string>("EventType")
            .HasValue<ModalityEvent>("Modality");

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

        modelBuilder.Entity<Project>()
            .HasDiscriminator<string>("ProjectType")
            .HasValue<AiProject>("AI")
            .HasValue<SustainabilityProject>("Sustainability");

        // ── 3. RELACIONES Y RESTRICCIONES ────────────────────────────────────
        modelBuilder.Entity<Category>()
            .HasOne<Event>()
            .WithMany()
            .HasForeignKey(c => c.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Project>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Projects)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Category>(e =>
        {
            e.Property(c => c.WeightCriterionA).HasPrecision(5, 2);
            e.Property(c => c.WeightCriterionB).HasPrecision(5, 2);
        });

        modelBuilder.Entity<Project>(e =>
        {
            e.Property(p => p.CriterionA).HasPrecision(5, 2);
            e.Property(p => p.CriterionB).HasPrecision(5, 2);
        });
    }
}