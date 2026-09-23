using backend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => x.UserId);
        builder.HasOne(x => x.User).WithMany(x => x.Projects).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
