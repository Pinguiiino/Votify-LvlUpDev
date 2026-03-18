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

    // ── Conjuntos principales ──────────────────────────────────────────────
    public DbSet<User> Usuarios { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Criterion> Criteria { get; set; }
    public DbSet<Prize> Prizes { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<ProjectCategory> ProjectCategories { get; set; }
    public DbSet<CriterionScore> CriterionScores { get; set; }
    public DbSet<VotingSession> VotingSessions { get; set; }
    public DbSet<Vote> Votes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── 1. JERARQUÍAS TPH ─────────────────────────────────────────────

        modelBuilder.Entity<Event>()
            .HasDiscriminator<string>("EventType")
            .HasValue<ModalityEvent>("Modality");

        modelBuilder.Entity<Project>()
            .HasDiscriminator<string>("ProjectType")
            .HasValue<AiProject>("AI")
            .HasValue<SustainabilityProject>("Sustainability");

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

        // Voter es abstracto pero no tiene tabla propia en TPH: registramos
        // las hojas directamente; EF las mapea bajo User.
        modelBuilder.Entity<Voter>().HasBaseType<User>();
        modelBuilder.Entity<Jury>().HasBaseType<Voter>();
        modelBuilder.Entity<Participant>().HasBaseType<Voter>();
        modelBuilder.Entity<Public>().HasBaseType<Voter>();

        // ── 2. RELACIONES ─────────────────────────────────────────────────

        // Category → Event  (1-N, cascade)
        modelBuilder.Entity<Category>()
            .HasOne<Event>()
            .WithMany()
            .HasForeignKey(c => c.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // Criterion → Category  (1-N, cascade)
        modelBuilder.Entity<Criterion>()
            .HasOne(cr => cr.Category)
            .WithMany(c => c.Criteria)
            .HasForeignKey(cr => cr.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Prize → Category  (1-N, cascade)
        modelBuilder.Entity<Prize>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Prizes)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // ProjectCategory — clave compuesta
        modelBuilder.Entity<ProjectCategory>()
            .HasKey(pc => pc.Id);          // Id propio (GUID)

        modelBuilder.Entity<ProjectCategory>()
            .HasIndex(pc => new { pc.ProjectId, pc.CategoryId })
            .IsUnique();

        modelBuilder.Entity<ProjectCategory>()
            .HasOne(pc => pc.Project)
            .WithMany(p => p.ProjectCategories)
            .HasForeignKey(pc => pc.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectCategory>()
            .HasOne(pc => pc.Category)
            .WithMany(c => c.ProjectCategories)
            .HasForeignKey(pc => pc.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // CriterionScore → ProjectCategory  (1-N, cascade)
        modelBuilder.Entity<CriterionScore>()
            .HasOne(cs => cs.ProjectCategory)
            .WithMany(pc => pc.CriterionScores)
            .HasForeignKey(cs => cs.ProjectCategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // CriterionScore → Criterion  (N-1, restrict)
        modelBuilder.Entity<CriterionScore>()
            .HasOne(cs => cs.Criterion)
            .WithMany()
            .HasForeignKey(cs => cs.CriterionId)
            .OnDelete(DeleteBehavior.Restrict);

        // VotingSession → Event  (1-N, cascade)
        modelBuilder.Entity<VotingSession>()
            .HasOne<Event>()
            .WithMany(e => e.VotingSessions)
            .HasForeignKey(vs => vs.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // Vote → VotingSession  (N-1)
        modelBuilder.Entity<Vote>()
            .HasOne(v => v.VotingSession)
            .WithMany(vs => vs.Votes)
            .HasForeignKey(v => v.VotingSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── 3. PRECISIÓN NUMÉRICA ─────────────────────────────────────────

        modelBuilder.Entity<ExpertVote>()
            .Property(e => e.RawScore)
            .HasPrecision(5, 2);

        modelBuilder.Entity<PublicVote>()
            .Property(p => p.RawScore)
            .HasPrecision(5, 2);

        modelBuilder.Entity<Criterion>()
            .Property(c => c.Weight)
            .HasPrecision(5, 4);

        modelBuilder.Entity<CriterionScore>()
            .Property(cs => cs.Score)
            .HasPrecision(5, 2);
    }
}