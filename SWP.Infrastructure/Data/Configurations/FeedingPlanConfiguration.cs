using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Data.Configurations;

public class FeedingPlanConfiguration : IEntityTypeConfiguration<FeedingPlan>
{
    public void Configure(EntityTypeBuilder<FeedingPlan> entity)
    {
        entity.HasKey(e => e.FeedingPlanId).HasName("PK__FeedingP__2DCC96AF5402805A");

        entity.ToTable("FeedingPlan");

        entity.Property(e => e.FeedingPlanId).HasColumnName("FeedingPlanID");
        entity.Property(e => e.CreatedAt).HasColumnType("datetime");
        entity.Property(e => e.HorseId).HasColumnName("HorseID");
        entity.Property(e => e.PlanName).HasMaxLength(150);
        entity.Property(e => e.SpecialInstructions).HasMaxLength(1000);
        entity.Property(e => e.Status)
            .HasMaxLength(30)
            .IsUnicode(false);
        entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.HasOne(d => d.Horse).WithMany(p => p.FeedingPlans)
            .HasForeignKey(d => d.HorseId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK__FeedingPl__Horse__4CA06362");

        entity.HasOne(d => d.User).WithMany(p => p.FeedingPlans)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK__FeedingPl__UserI__4D94879B");
    }
}
