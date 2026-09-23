using backend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class ExternalAuthTicketConfiguration : IEntityTypeConfiguration<ExternalAuthTicket>
{
    public void Configure(EntityTypeBuilder<ExternalAuthTicket> builder)
    {
        builder.Property(x => x.CodeHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.CodeHash).IsUnique();
        builder.HasIndex(x => x.ExpiresAt);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
