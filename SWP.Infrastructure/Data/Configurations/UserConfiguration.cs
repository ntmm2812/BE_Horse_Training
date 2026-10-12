using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> entity)
    {
        entity.HasKey(e => e.UserId).HasName("PK__Users__1788CCAC35C85A4D");

        entity.HasIndex(e => e.Email, "UQ__Users__A9D10534EDA32067").IsUnique();

        entity.Property(e => e.UserId).HasColumnName("UserID");
        entity.Property(e => e.Address).HasMaxLength(255);
        entity.Property(e => e.CreatedAt).HasColumnType("datetime");
        entity.Property(e => e.Email)
            .HasMaxLength(255)
            .IsUnicode(false);
        entity.Property(e => e.FullName).HasMaxLength(100);
        entity.Property(e => e.Gender)
            .HasMaxLength(20)
            .IsUnicode(false);
        entity.Property(e => e.GoogleId).HasColumnName("GoogleID");
        entity.Property(e => e.PasswordHash)
            .HasMaxLength(255)
            .IsUnicode(false);
        entity.Property(e => e.PhoneNumber)
            .HasMaxLength(20)
            .IsUnicode(false);
        entity.Property(e => e.RoleId).HasColumnName("RoleID");
        entity.Property(e => e.Status)
            .HasMaxLength(20)
            .IsUnicode(false);
        entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

        entity.HasOne(d => d.Role).WithMany(p => p.Users)
            .HasForeignKey(d => d.RoleId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK__Users__RoleID__2F10007B");
    }
}
