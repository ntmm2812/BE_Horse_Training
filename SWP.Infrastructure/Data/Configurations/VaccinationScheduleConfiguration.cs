using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Data.Configurations;

public class VaccinationScheduleConfiguration : IEntityTypeConfiguration<VaccinationSchedule>
{
    public void Configure(EntityTypeBuilder<VaccinationSchedule> entity)
    {
        entity.HasKey(e => e.ScheduleId);
        entity.ToTable("VaccinationSchedule");

        entity.Property(e => e.ScheduleId).HasColumnName("ScheduleID");
        entity.Property(e => e.HorseId).HasColumnName("HorseID");
        entity.Property(e => e.Loai).HasMaxLength(50).IsRequired();
        entity.Property(e => e.NgayHen).HasColumnType("date");
        entity.Property(e => e.TrangThai).HasMaxLength(30).IsRequired();
        entity.Property(e => e.GhiChu).HasMaxLength(500);

        entity.HasOne(d => d.Horse)
            .WithMany(p => p.VaccinationSchedules)
            .HasForeignKey(d => d.HorseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
