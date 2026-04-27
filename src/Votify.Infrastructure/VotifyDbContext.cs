using Microsoft.EntityFrameworkCore;
using Votify.Domain.CategoryFolder;
using Votify.Domain.EventFolder;
using Votify.Domain.ProjectFolder;
using Votify.Domain.UserFolder;
using Votify.Domain.VoteFolder;
using Votify.Domain.AuditFolder;

namespace Votify.Infrastructure;

public class VotifyDbContext : DbContext
{
    public VotifyDbContext(DbContextOptions<VotifyDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Criterion> Criteria { get; set; }
    public DbSet<Prize> Prizes { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<ProjectCategory> ProjectCategories { get; set; }
    public DbSet<CriterionScore> CriterionScores { get; set; }
    public DbSet<VotingSession> VotingSessions { get; set; }
    public DbSet<Vote> Votes { get; set; }
    public DbSet<AuditRequest> AuditRequests { get; set; }
    public DbSet<WeightedVote> WeightedVotes { get; set; }
    public DbSet<WeightedCriterionScore> WeightedCriterionScores { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Event>()
            .HasDiscriminator<string>("EventType")
            .HasValue<ModalityEvent>("Modality");

        modelBuilder.Entity<Project>()
            .HasDiscriminator<string>("ProjectType")
            .HasValue<AiProject>("AI")
            .HasValue<SustainabilityProject>("Sustainability")
            .HasValue<GeneralProject>("General");

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

        modelBuilder.Entity<Voter>().HasBaseType<User>();
        modelBuilder.Entity<Jury>().HasBaseType<Voter>();
        modelBuilder.Entity<Participant>().HasBaseType<Voter>();
        modelBuilder.Entity<Public>().HasBaseType<Voter>();

        modelBuilder.Entity<Category>()
            .HasOne<Event>()
            .WithMany()
            .HasForeignKey(c => c.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VotingSession>()
            .HasOne(vs => vs.Category)
            .WithMany(c => c.VotingSessions)
            .HasForeignKey(vs => vs.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Criterion>()
            .HasOne(cr => cr.VotingSession)
            .WithMany(vs => vs.Criteria)
            .HasForeignKey(cr => cr.VotingSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Prize>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Prizes)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectCategory>()
            .HasKey(pc => pc.Id);

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

        modelBuilder.Entity<CriterionScore>()
            .HasOne(cs => cs.ProjectCategory)
            .WithMany(pc => pc.CriterionScores)
            .HasForeignKey(cs => cs.ProjectCategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CriterionScore>()
            .HasOne(cs => cs.Criterion)
            .WithMany()
            .HasForeignKey(cs => cs.CriterionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Vote>()
            .HasOne(v => v.VotingSession)
            .WithMany(vs => vs.Votes)
            .HasForeignKey(v => v.VotingSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AuditRequest>()
            .HasIndex(a => a.ProjectId);

        modelBuilder.Entity<Category>()
            .Property(c => c.JuryWeight)
            .HasPrecision(5, 4);

        modelBuilder.Entity<Category>()
            .Property(c => c.PublicWeight)
            .HasPrecision(5, 4);

        modelBuilder.Entity<Criterion>()
            .Property(c => c.Weight)
            .HasPrecision(5, 4);

        modelBuilder.Entity<CriterionScore>()
            .Property(cs => cs.Score)
            .HasPrecision(5, 2);

        modelBuilder.Entity<WeightedVote>()
            .HasMany(wv => wv.CriterionScores)
            .WithOne(wcs => wcs.WeightedVote)
            .HasForeignKey(wcs => wcs.WeightedVoteId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WeightedCriterionScore>()
            .HasOne(wcs => wcs.WeightedVote)
            .WithMany(wv => wv.CriterionScores)
            .HasForeignKey(wcs => wcs.WeightedVoteId);

        modelBuilder.Entity<WeightedCriterionScore>()
            .Property(wcs => wcs.Score)
            .HasPrecision(5, 2);
    }
}
