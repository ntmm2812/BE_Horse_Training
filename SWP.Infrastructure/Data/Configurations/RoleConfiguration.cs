using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Data.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> entity)
    {
        entity.HasKey(e => e.RoleId).HasName("PK__Role__8AFACE3AC3DCC0E2");

        entity.ToTable("Role");

        entity.HasIndex(e => e.RoleName, "UQ__Role__8A2B6160BB80B5AE").IsUnique();

        entity.Property(e => e.RoleId).HasColumnName("RoleID");
        entity.Property(e => e.RoleName)
            .HasMaxLength(50)
            .IsUnicode(false);
    }
}
