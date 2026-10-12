using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Data.Configurations;

public class HorseHealthStatusConfiguration : IEntityTypeConfiguration<HorseHealthStatus>
{
    public void Configure(EntityTypeBuilder<HorseHealthStatus> entity)
    {
        entity.HasKey(e => e.HorseId);

        entity.ToTable("HorseHealthStatus");

        entity.Property(e => e.HorseId).HasColumnName("HorseID");
        entity.Property(e => e.TrangThai).HasMaxLength(30);
        entity.Property(e => e.NgayCapNhat).HasColumnType("datetime");

        entity.HasOne(d => d.Horse)
            .WithOne(p => p.HealthStatusRecord)
            .HasForeignKey<HorseHealthStatus>(d => d.HorseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
