using backend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class PositionAccessRuleConfiguration : IEntityTypeConfiguration<PositionAccessRule> {
    public void Configure(EntityTypeBuilder<PositionAccessRule> builder) {
        builder.Property(x => x.Operator).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.ComparisonValue).HasMaxLength(500).IsRequired();
        builder.Property(x => x.NumberValue).HasPrecision(18, 4);
        builder.HasIndex(x => x.PositionId);
        builder.HasOne(x => x.Position).WithMany(x => x.AccessRules).HasForeignKey(x => x.PositionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Attribute).WithMany().HasForeignKey(x => x.AttributeId).OnDelete(DeleteBehavior.Restrict);
    }
}
