using backend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.Property(x => x.FirstName).HasMaxLength(100);
        builder.Property(x => x.LastName).HasMaxLength(100);
        builder.Property(x => x.Location).HasMaxLength(200);
        builder.Property(x => x.PhotoObjectKey).HasMaxLength(500);
        builder.Property(x => x.Language).HasMaxLength(8);
        builder.Property(x => x.Theme).HasMaxLength(16);
        builder.Property(x => x.AuthVersion).IsConcurrencyToken();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => x.NormalizedEmail).IsUnique();
        builder.HasGeneratedTsVectorColumn(x => x.SearchVector, "english", x => new { x.FirstName, x.LastName });
        builder.HasIndex(x => x.SearchVector).HasMethod("GIN");
    }
}
