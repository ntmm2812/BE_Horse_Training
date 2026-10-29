using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Data.Configurations;

public class SupplyConfiguration : IEntityTypeConfiguration<Supply>
{
    public void Configure(EntityTypeBuilder<Supply> entity)
    {
        entity.HasKey(e => e.SupplyId).HasName("PK__Supply__7CDD6C8EBB7DD76E");

        entity.ToTable("Supply");

        entity.Property(e => e.SupplyId).HasColumnName("SupplyID");
        entity.Property(e => e.Category).HasMaxLength(50);
        entity.Property(e => e.CreatedAt).HasColumnType("datetime");
        entity.Property(e => e.Description).HasMaxLength(500);
        entity.Property(e => e.QuantityInStock).HasColumnType("decimal(12, 2)");
        entity.Property(e => e.Status)
            .HasMaxLength(30)
            .IsUnicode(false);
        entity.Property(e => e.StorageLocation).HasMaxLength(100);
        entity.Property(e => e.SupplyName).HasMaxLength(150);
        entity.Property(e => e.Unit)
            .HasMaxLength(20)
            .IsUnicode(false);
        entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");
        entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.HasOne(d => d.User).WithMany(p => p.Supplies)
            .HasForeignKey(d => d.UserId)
            .HasConstraintName("FK__Supply__UserID__534D60F1");
    }
}
