using backend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class ProjectTagConfiguration : IEntityTypeConfiguration<ProjectTag>
{
    public void Configure(EntityTypeBuilder<ProjectTag> builder)
    {
        builder.HasKey(x => new { x.ProjectId, x.TagId });
        builder.HasOne(x => x.Project).WithMany(x => x.Tags).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Tag).WithMany(x => x.Projects).HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Restrict);
    }
}
