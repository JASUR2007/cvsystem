using backend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class AttributeOptionConfiguration : IEntityTypeConfiguration<AttributeOption> {
    public void Configure(EntityTypeBuilder<AttributeOption> builder) {
        builder.Property(x => x.Value).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => new { x.AttributeId, x.Value }).IsUnique();
        builder.HasOne(x => x.Attribute).WithMany(x => x.Options).HasForeignKey(x => x.AttributeId).OnDelete(DeleteBehavior.Cascade);
    }
}
