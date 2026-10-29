using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Data.Configurations;

public class IncidentReportConfiguration : IEntityTypeConfiguration<IncidentReport>
{
    public void Configure(EntityTypeBuilder<IncidentReport> entity)
    {
        entity.ToTable("IncidentReport");
        entity.HasKey(e => e.ReportId);
        entity.Property(e => e.ReportId).HasColumnName("ReportID").ValueGeneratedOnAdd();
        entity.Property(e => e.HorseId).HasColumnName("HorseID");
        entity.Property(e => e.GroomId).HasColumnName("GroomID");
        entity.Property(e => e.IncidentType).HasColumnName("LoaiSuCo").HasMaxLength(50);
        entity.Property(e => e.Description).HasColumnName("MoTa").HasMaxLength(300);
        entity.Property(e => e.ImageUrl).HasColumnName("HinhAnh");
        entity.Property(e => e.ReportedOn).HasColumnName("Ngay").HasColumnType("date");
        entity.Property(e => e.CreatedAt).HasColumnType("datetime");

        entity.HasOne(d => d.Horse)
            .WithMany(p => p.IncidentReports)
            .HasForeignKey(d => d.HorseId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(d => d.Groom)
            .WithMany(p => p.IncidentReports)
            .HasForeignKey(d => d.GroomId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
