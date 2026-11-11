using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Data.Configurations;

public class TrainingSessionReportConfiguration : IEntityTypeConfiguration<TrainingSessionReport>
{
    public void Configure(EntityTypeBuilder<TrainingSessionReport> entity)
    {
        entity.ToTable("TrainingSessionReport");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Result).HasMaxLength(20);
        entity.Property(x => x.ActualDistanceKm).HasColumnType("decimal(8, 2)");
        entity.Property(x => x.HorseReaction).HasMaxLength(20);
        entity.Property(x => x.Note).HasMaxLength(300);
        entity.HasOne(x => x.Schedule).WithOne(s => s.Report).HasForeignKey<TrainingSessionReport>(x => x.ScheduleId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(x => x.ReportedByUser).WithMany().HasForeignKey(x => x.ReportedByUserId).OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(x => x.ScheduleId).IsUnique();
    }
}
