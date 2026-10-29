using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Data.Configurations;

public class VaccinationScheduleConfiguration : IEntityTypeConfiguration<VaccinationSchedule>
{
    public void Configure(EntityTypeBuilder<VaccinationSchedule> entity)
    {
        entity.ToTable("VaccinationSchedule");
        entity.HasKey(e => e.ScheduleId);
        entity.Property(e => e.ScheduleId).HasColumnName("ScheduleID").ValueGeneratedOnAdd();
        entity.Property(e => e.HorseId).HasColumnName("HorseID");
        entity.Property(e => e.VaccineType).HasColumnName("Loai").HasMaxLength(50);
        entity.Property(e => e.DueDate).HasColumnName("NgayHen").HasColumnType("date");
        entity.Property(e => e.Status).HasColumnName("TrangThai").HasMaxLength(20);
        entity.Property(e => e.CreatedAt).HasColumnType("datetime");

        entity.HasOne(d => d.Horse)
            .WithMany(p => p.VaccinationSchedules)
            .HasForeignKey(d => d.HorseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
