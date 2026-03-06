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

        // ── Herencia de Vote (Table-Per-Hierarchy) ──────────────────────────
        modelBuilder.Entity<Vote>()
            .HasDiscriminator<string>("VoteType")
            .HasValue<ExpertVote>("Expert")
            .HasValue<PublicVote>("Public");

        // ── Herencia de User (Table-Per-Hierarchy) ──────────────────────────
        modelBuilder.Entity<User>()
            .HasDiscriminator<string>("TipoUsuario")
            .HasValue<Organizer>("Organizador")
            .HasValue<Public>("PublicoGeneral")
            .HasValue<Jury>("Jurado")
            .HasValue<Participant>("Participante");

        // ── Herencia de Project (Table-Per-Hierarchy) ───────────────────────
        modelBuilder.Entity<Project>()
            .HasDiscriminator<string>("ProjectType")
            .HasValue<AiProject>("AI")
            .HasValue<SustainabilityProject>("Sustainability");

        // ── Relaciones ───────────────────────────────────────────────────────

        // Event (1) ──── (*) Category
        modelBuilder.Entity<Category>()
            .HasOne<Event>()
            .WithMany()
            .HasForeignKey(c => c.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // Category (1) ──── (*) Project
        modelBuilder.Entity<Project>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Projects)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Precisión de decimales ───────────────────────────────────────────
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