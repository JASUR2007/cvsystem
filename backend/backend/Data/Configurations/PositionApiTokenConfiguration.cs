using backend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class PositionApiTokenConfiguration : IEntityTypeConfiguration<PositionApiToken> {
    public void Configure(EntityTypeBuilder<PositionApiToken> builder) {
        builder.ToTable("PositionApiTokens");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TokenHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(x => x.PositionId)
            .IsUnique();

        builder.HasIndex(x => x.TokenHash);

        builder.HasOne(x => x.Position)
            .WithOne(x => x.ApiToken)
            .HasForeignKey<PositionApiToken>(x => x.PositionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
