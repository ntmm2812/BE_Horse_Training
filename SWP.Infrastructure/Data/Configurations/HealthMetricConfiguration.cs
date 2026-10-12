using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Data.Configurations;

public class HealthMetricConfiguration : IEntityTypeConfiguration<HealthMetric>
{
    public void Configure(EntityTypeBuilder<HealthMetric> entity)
    {
        entity.HasKey(e => e.MetricId).HasName("PK__HealthMe__561056457FAB29C7");

        entity.ToTable("HealthMetric");

        entity.Property(e => e.MetricId).HasColumnName("MetricID");
        entity.Property(e => e.BloodOxygen).HasColumnType("decimal(5, 2)");
        entity.Property(e => e.BodyTemperature).HasColumnType("decimal(4, 2)");
        entity.Property(e => e.DataSource)
            .HasMaxLength(50)
            .IsUnicode(false);
        entity.Property(e => e.DistanceKm).HasColumnType("decimal(8, 2)");
        entity.Property(e => e.HeartRate).HasColumnType("decimal(6, 2)");
        entity.Property(e => e.HorseId).HasColumnName("HorseID");
        entity.Property(e => e.Notes).HasMaxLength(500);
        entity.Property(e => e.RecordedAt).HasColumnType("datetime");
        entity.Property(e => e.RespiratoryRate).HasColumnType("decimal(6, 2)");
        entity.Property(e => e.SpeedKmh).HasColumnType("decimal(6, 2)");
        entity.Property(e => e.StressLevel)
            .HasMaxLength(20)
            .IsUnicode(false);

        entity.HasOne(d => d.Horse).WithMany(p => p.HealthMetrics)
            .HasForeignKey(d => d.HorseId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK__HealthMet__Horse__3C69FB99");
    }
}
