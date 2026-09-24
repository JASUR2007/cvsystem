using backend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class PositionProjectTagConfiguration : IEntityTypeConfiguration<PositionProjectTag> {
    public void Configure(EntityTypeBuilder<PositionProjectTag> builder) {
        builder.HasKey(x => new { x.PositionId, x.TagId });
        builder.HasOne(x => x.Position).WithMany(x => x.ProjectTags).HasForeignKey(x => x.PositionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Tag).WithMany(x => x.Positions).HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Restrict);
    }
}
