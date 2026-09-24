using backend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class PositionConfiguration : IEntityTypeConfiguration<Position> {
    public void Configure(EntityTypeBuilder<Position> builder) {
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ShortDescription).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Company).HasMaxLength(160);
        builder.Property(x => x.Level).HasConversion<string>().HasMaxLength(24);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => x.UpdatedAt);
        builder.HasGeneratedTsVectorColumn(x => x.SearchVector, "english", x => new { x.Title, x.ShortDescription });
        builder.HasIndex(x => x.SearchVector).HasMethod("GIN");
    }
}
