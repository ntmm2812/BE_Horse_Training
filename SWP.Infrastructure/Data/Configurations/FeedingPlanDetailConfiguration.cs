using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Data.Configurations;

public class FeedingPlanDetailConfiguration : IEntityTypeConfiguration<FeedingPlanDetail>
{
    public void Configure(EntityTypeBuilder<FeedingPlanDetail> entity)
    {
        entity.HasKey(e => e.FeedingPlanDetailId).HasName("PK__FeedingP__D62F0E6F4F9F0164");

        entity.ToTable("FeedingPlanDetail");

        entity.Property(e => e.FeedingPlanDetailId).HasColumnName("FeedingPlanDetailID");
        entity.Property(e => e.Calories).HasColumnType("decimal(10, 2)");
        entity.Property(e => e.CreatedAt).HasColumnType("datetime");
        entity.Property(e => e.FeedName).HasMaxLength(150);
        entity.Property(e => e.FeedingPlanId).HasColumnName("FeedingPlanID");
        entity.Property(e => e.Instructions).HasMaxLength(500);
        entity.Property(e => e.MealName).HasMaxLength(100);
        entity.Property(e => e.Quantity).HasColumnType("decimal(10, 2)");
        entity.Property(e => e.Unit)
            .HasMaxLength(20)
            .IsUnicode(false);

        entity.HasOne(d => d.FeedingPlan).WithMany(p => p.FeedingPlanDetails)
            .HasForeignKey(d => d.FeedingPlanId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK__FeedingPl__Feedi__5070F446");
    }
}
