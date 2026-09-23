using backend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class DiscussionPostConfiguration : IEntityTypeConfiguration<DiscussionPost>
{
    public void Configure(EntityTypeBuilder<DiscussionPost> builder)
    {
        builder.Property(x => x.Content).IsRequired();
        builder.HasIndex(x => new { x.PositionId, x.CreatedAt });
        builder.HasOne(x => x.Position).WithMany(x => x.DiscussionPosts).HasForeignKey(x => x.PositionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
    }
}
