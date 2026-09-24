using backend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class PositionAttributeConfiguration : IEntityTypeConfiguration<PositionAttribute> {
    public void Configure(EntityTypeBuilder<PositionAttribute> builder) {
        builder.HasKey(x => new { x.PositionId, x.AttributeId });
        builder.HasOne(x => x.Position).WithMany(x => x.Attributes).HasForeignKey(x => x.PositionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Attribute).WithMany(x => x.PositionAttributes).HasForeignKey(x => x.AttributeId).OnDelete(DeleteBehavior.Restrict);
    }
}
