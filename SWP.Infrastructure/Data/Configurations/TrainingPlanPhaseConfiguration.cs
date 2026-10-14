using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Data.Configurations;

public class TrainingPlanPhaseConfiguration : IEntityTypeConfiguration<TrainingPlanPhase>
{
    public void Configure(EntityTypeBuilder<TrainingPlanPhase> entity)
    {
        entity.HasKey(e => e.PhaseId);

        entity.ToTable("TrainingPlanPhase");

        entity.Property(e => e.PhaseId).HasColumnName("PhaseID");
        entity.Property(e => e.PlanId).HasColumnName("PlanID");
        entity.Property(e => e.PhaseName).HasMaxLength(100);
        entity.Property(e => e.Goal).HasMaxLength(500);
        entity.Property(e => e.IntensityLevel)
            .HasMaxLength(20)
            .IsUnicode(false);
        entity.Property(e => e.TargetDistanceKm).HasColumnType("decimal(8, 2)");
        entity.Property(e => e.Surface)
            .HasMaxLength(30)
            .IsUnicode(false);
        entity.Property(e => e.CreatedAt).HasColumnType("datetime");

        entity.HasOne(d => d.Plan).WithMany(p => p.Phases)
            .HasForeignKey(d => d.PlanId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_TrainingPlanPhase_TrainingPlans");
    }
}
