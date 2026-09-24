using backend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class TagConfiguration : IEntityTypeConfiguration<Tag> {
    public void Configure(EntityTypeBuilder<Tag> builder) {
        builder.Property(x => x.Name).HasMaxLength(80).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
    }
}
