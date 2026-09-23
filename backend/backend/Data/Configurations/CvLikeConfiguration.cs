using backend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class CvLikeConfiguration : IEntityTypeConfiguration<CvLike>
{
    public void Configure(EntityTypeBuilder<CvLike> builder)
    {
        builder.HasKey(x => new { x.CvId, x.RecruiterId });
        builder.HasOne(x => x.Cv).WithMany(x => x.Likes).HasForeignKey(x => x.CvId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Recruiter).WithMany().HasForeignKey(x => x.RecruiterId).OnDelete(DeleteBehavior.Restrict);
    }
}
