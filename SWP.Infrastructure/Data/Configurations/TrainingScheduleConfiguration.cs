using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Data.Configurations;

public class TrainingScheduleConfiguration : IEntityTypeConfiguration<TrainingSchedule>
{
    public void Configure(EntityTypeBuilder<TrainingSchedule> entity)
    {
        entity.HasKey(e => e.ScheduleId).HasName("PK__Training__9C8A5B6901457999");

        entity.ToTable("TrainingSchedule");

        entity.Property(e => e.ScheduleId).HasColumnName("ScheduleID");
        entity.Property(e => e.AssignedToUserId).HasColumnName("AssignedToUserID");
        entity.Property(e => e.CreatedAt).HasColumnType("datetime");
        entity.Property(e => e.DistanceKm).HasColumnType("decimal(8, 2)");
        entity.Property(e => e.EndTime).HasColumnType("datetime");
        entity.Property(e => e.Notes).HasMaxLength(1000);
        entity.Property(e => e.PerformanceScore).HasColumnType("decimal(5, 2)");
        entity.Property(e => e.PlanId).HasColumnName("PlanID");
        entity.Property(e => e.Result).HasMaxLength(500);
        entity.Property(e => e.SessionTitle).HasMaxLength(150);
        entity.Property(e => e.StartTime).HasColumnType("datetime");
        entity.Property(e => e.Status)
            .HasMaxLength(30)
            .IsUnicode(false);
        entity.Property(e => e.TrainingDate).HasColumnType("datetime");
        entity.Property(e => e.TrainingType)
            .HasMaxLength(50)
            .IsUnicode(false);
        entity.Property(e => e.TrialVideoUrl).HasMaxLength(500);

        entity.HasOne(d => d.AssignedToUser).WithMany(p => p.TrainingSchedules)
            .HasForeignKey(d => d.AssignedToUserId)
            .HasConstraintName("FK__TrainingS__Assig__398D8EEE");

        entity.HasOne(d => d.Plan).WithMany(p => p.TrainingSchedules)
            .HasForeignKey(d => d.PlanId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK__TrainingS__PlanI__38996AB5");
    }
}
