using backend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class CvConfiguration : IEntityTypeConfiguration<Cv>
{
    public void Configure(EntityTypeBuilder<Cv> builder)
    {
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(24);
        builder.HasIndex(x => new { x.CandidateId, x.PositionId }).IsUnique();
        builder.HasIndex(x => x.PositionId);
        builder.HasOne(x => x.Candidate).WithMany(x => x.Cvs).HasForeignKey(x => x.CandidateId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Position).WithMany(x => x.Cvs).HasForeignKey(x => x.PositionId).OnDelete(DeleteBehavior.Cascade);
    }
}
