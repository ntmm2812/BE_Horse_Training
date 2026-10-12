using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> entity)
    {
        entity.HasKey(e => e.LogId).HasName("PK__AuditLog__5E5499A8D5D1FB83");

        entity.ToTable("AuditLog");

        entity.Property(e => e.LogId).HasColumnName("LogID");
        entity.Property(e => e.Action)
            .HasMaxLength(50)
            .IsUnicode(false);
        entity.Property(e => e.CreatedAt).HasColumnType("datetime");
        entity.Property(e => e.Ipaddress)
            .HasMaxLength(45)
            .IsUnicode(false)
            .HasColumnName("IPAddress");
        entity.Property(e => e.RecordId).HasColumnName("RecordID");
        entity.Property(e => e.TableName)
            .HasMaxLength(50)
            .IsUnicode(false);
        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
            .HasForeignKey(d => d.UserId)
            .HasConstraintName("FK__AuditLog__UserID__628FA481");
    }
}
