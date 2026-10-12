using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Data.Configurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> entity)
    {
        entity.HasKey(e => e.RolePermissionId).HasName("PK__RolePerm__120F469A50F6C98C");

        entity.ToTable("RolePermission");

        entity.Property(e => e.RolePermissionId).HasColumnName("RolePermissionID");
        entity.Property(e => e.CreatedAt).HasColumnType("datetime");
        entity.Property(e => e.PermissionId).HasColumnName("PermissionID");
        entity.Property(e => e.RoleId).HasColumnName("RoleID");

        entity.HasOne(d => d.Permission).WithMany(p => p.RolePermissions)
            .HasForeignKey(d => d.PermissionId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK__RolePermi__Permi__2B3F6F97");

        entity.HasOne(d => d.Role).WithMany(p => p.RolePermissions)
            .HasForeignKey(d => d.RoleId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK__RolePermi__Creat__2A4B4B5E");
    }
}
