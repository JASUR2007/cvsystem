using backend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class AttributeDefinitionConfiguration : IEntityTypeConfiguration<AttributeDefinition>
{
    public void Configure(EntityTypeBuilder<AttributeDefinition> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(24);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => x.Name).IsUnique();
    }
}
