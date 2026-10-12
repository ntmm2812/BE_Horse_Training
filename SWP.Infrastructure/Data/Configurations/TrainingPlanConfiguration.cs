using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Data.Configurations;

public class TrainingPlanConfiguration : IEntityTypeConfiguration<TrainingPlan>
{
    public void Configure(EntityTypeBuilder<TrainingPlan> entity)
    {
        entity.HasKey(e => e.PlanId).HasName("PK__Training__755C22D75CCB085B");

        entity.Property(e => e.PlanId).HasColumnName("PlanID");
        entity.Property(e => e.CreatedAt).HasColumnType("datetime");
        entity.Property(e => e.HorseId).HasColumnName("HorseID");
        entity.Property(e => e.IntensityLevel)
            .HasMaxLength(20)
            .IsUnicode(false);
        entity.Property(e => e.Notes).HasMaxLength(1000);
        entity.Property(e => e.Objective).HasMaxLength(500);
        entity.Property(e => e.PlanName).HasMaxLength(150);
        entity.Property(e => e.PlanStatus)
            .HasMaxLength(30)
            .IsUnicode(false);
        entity.Property(e => e.TrainingType)
            .HasMaxLength(50)
            .IsUnicode(false);
        entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

        entity.HasOne(d => d.Horse).WithMany(p => p.TrainingPlans)
            .HasForeignKey(d => d.HorseId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK__TrainingP__Horse__35BCFE0A");
    }
}
