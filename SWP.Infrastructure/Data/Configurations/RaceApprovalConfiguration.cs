using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Data.Configurations;

public class RaceApprovalConfiguration : IEntityTypeConfiguration<RaceApproval>
{
    public void Configure(EntityTypeBuilder<RaceApproval> entity)
    {
        entity.ToTable("RaceApproval");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Status).HasMaxLength(30);
        entity.Property(x => x.Note).HasMaxLength(300);
        entity.Property(x => x.RejectReason).HasMaxLength(300);
        entity.Property(x => x.RejectedBy).HasMaxLength(20);
        entity.Property(x => x.Version).HasMaxLength(36).IsConcurrencyToken();
        entity.HasOne(x => x.Horse).WithMany().HasForeignKey(x => x.HorseId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Race).WithMany().HasForeignKey(x => x.RaceId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Trainer).WithMany().HasForeignKey(x => x.TrainerId).OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(x => new { x.HorseId, x.RaceId });
    }
}
