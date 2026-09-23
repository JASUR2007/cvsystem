using backend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class UserAttributeValueConfiguration : IEntityTypeConfiguration<UserAttributeValue>
{
    public void Configure(EntityTypeBuilder<UserAttributeValue> builder)
    {
        builder.HasKey(x => new { x.UserId, x.AttributeId });
        builder.Property(x => x.NumberValue).HasPrecision(18, 4);
        builder.Property(x => x.ImageObjectKey);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => x.AttributeId);
        builder.HasOne(x => x.User).WithMany(x => x.AttributeValues).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Attribute).WithMany(x => x.UserValues).HasForeignKey(x => x.AttributeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SelectedOption).WithMany().HasForeignKey(x => x.SelectedOptionId).OnDelete(DeleteBehavior.Restrict);
    }
}
