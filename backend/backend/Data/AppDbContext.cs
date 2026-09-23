using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace backend.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<AttributeDefinition> Attributes => Set<AttributeDefinition>();
    public DbSet<AttributeOption> AttributeOptions => Set<AttributeOption>();
    public DbSet<UserAttributeValue> UserAttributeValues => Set<UserAttributeValue>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<PositionAttribute> PositionAttributes => Set<PositionAttribute>();
    public DbSet<PositionAccessRule> PositionAccessRules => Set<PositionAccessRule>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ProjectTag> ProjectTags => Set<ProjectTag>();
    public DbSet<PositionProjectTag> PositionProjectTags => Set<PositionProjectTag>();
    public DbSet<Cv> Cvs => Set<Cv>();
    public DbSet<CvLike> CvLikes => Set<CvLike>();
    public DbSet<DiscussionPost> DiscussionPosts => Set<DiscussionPost>();
    public DbSet<ExternalAuthTicket> ExternalAuthTickets => Set<ExternalAuthTicket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.Property(x => x.FirstName).HasMaxLength(100);
            entity.Property(x => x.LastName).HasMaxLength(100);
            entity.Property(x => x.Location).HasMaxLength(200);
            entity.Property(x => x.PhotoObjectKey).HasMaxLength(500);
            entity.Property(x => x.Language).HasMaxLength(8);
            entity.Property(x => x.Theme).HasMaxLength(16);
            entity.Property(x => x.AuthVersion).IsConcurrencyToken();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => x.NormalizedEmail).IsUnique();
            entity.HasGeneratedTsVectorColumn(x => x.SearchVector, "english", x => new { x.FirstName, x.LastName });
            entity.HasIndex(x => x.SearchVector).HasMethod("GIN");
        });

        modelBuilder.Entity<AttributeDefinition>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(24);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<AttributeOption>(entity =>
        {
            entity.Property(x => x.Value).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => new { x.AttributeId, x.Value }).IsUnique();
            entity.HasOne(x => x.Attribute).WithMany(x => x.Options).HasForeignKey(x => x.AttributeId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserAttributeValue>(entity =>
        {
            entity.HasKey(x => new { x.UserId, x.AttributeId });
            entity.Property(x => x.NumberValue).HasPrecision(18, 4);
            entity.Property(x => x.ImageObjectKey).HasMaxLength(500);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => x.AttributeId);
            entity.HasOne(x => x.User).WithMany(x => x.AttributeValues).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Attribute).WithMany(x => x.UserValues).HasForeignKey(x => x.AttributeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.SelectedOption).WithMany().HasForeignKey(x => x.SelectedOptionId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Position>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ShortDescription).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.Company).HasMaxLength(160);
            entity.Property(x => x.Level).HasConversion<string>().HasMaxLength(24);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => x.UpdatedAt);
            entity.HasGeneratedTsVectorColumn(x => x.SearchVector, "english", x => new { x.Title, x.ShortDescription });
            entity.HasIndex(x => x.SearchVector).HasMethod("GIN");
        });

        modelBuilder.Entity<PositionAttribute>(entity =>
        {
            entity.HasKey(x => new { x.PositionId, x.AttributeId });
            entity.HasOne(x => x.Position).WithMany(x => x.Attributes).HasForeignKey(x => x.PositionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Attribute).WithMany(x => x.PositionAttributes).HasForeignKey(x => x.AttributeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PositionAccessRule>(entity =>
        {
            entity.Property(x => x.Operator).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.ComparisonValue).HasMaxLength(500).IsRequired();
            entity.Property(x => x.NumberValue).HasPrecision(18, 4);
            entity.HasIndex(x => x.PositionId);
            entity.HasOne(x => x.Position).WithMany(x => x.AccessRules).HasForeignKey(x => x.PositionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Attribute).WithMany().HasForeignKey(x => x.AttributeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => x.UserId);
            entity.HasOne(x => x.User).WithMany(x => x.Projects).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(80).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<ProjectTag>(entity =>
        {
            entity.HasKey(x => new { x.ProjectId, x.TagId });
            entity.HasOne(x => x.Project).WithMany(x => x.Tags).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Tag).WithMany(x => x.Projects).HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PositionProjectTag>(entity =>
        {
            entity.HasKey(x => new { x.PositionId, x.TagId });
            entity.HasOne(x => x.Position).WithMany(x => x.ProjectTags).HasForeignKey(x => x.PositionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Tag).WithMany(x => x.Positions).HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Cv>(entity =>
        {
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(24);
            entity.HasIndex(x => new { x.CandidateId, x.PositionId }).IsUnique();
            entity.HasIndex(x => x.PositionId);
            entity.HasOne(x => x.Candidate).WithMany(x => x.Cvs).HasForeignKey(x => x.CandidateId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Position).WithMany(x => x.Cvs).HasForeignKey(x => x.PositionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CvLike>(entity =>
        {
            entity.HasKey(x => new { x.CvId, x.RecruiterId });
            entity.HasOne(x => x.Cv).WithMany(x => x.Likes).HasForeignKey(x => x.CvId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Recruiter).WithMany().HasForeignKey(x => x.RecruiterId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DiscussionPost>(entity =>
        {
            entity.Property(x => x.Content).IsRequired();
            entity.HasIndex(x => new { x.PositionId, x.CreatedAt });
            entity.HasOne(x => x.Position).WithMany(x => x.DiscussionPosts).HasForeignKey(x => x.PositionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ExternalAuthTicket>(entity =>
        {
            entity.Property(x => x.CodeHash).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => x.CodeHash).IsUnique();
            entity.HasIndex(x => x.ExpiresAt);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
