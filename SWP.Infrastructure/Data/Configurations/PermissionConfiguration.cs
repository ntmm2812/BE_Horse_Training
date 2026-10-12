using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Data.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> entity)
    {
        entity.HasKey(e => e.PermissionId).HasName("PK__Permissi__EFA6FB0FD92215EE");

        entity.ToTable("Permission");

        entity.HasIndex(e => e.PermissionName, "UQ__Permissi__0FFDA357AED5C445").IsUnique();

        entity.Property(e => e.PermissionId).HasColumnName("PermissionID");
        entity.Property(e => e.CreatedAt).HasColumnType("datetime");
        entity.Property(e => e.Description).HasMaxLength(255);
        entity.Property(e => e.PermissionName)
            .HasMaxLength(100)
            .IsUnicode(false);
    }
}
