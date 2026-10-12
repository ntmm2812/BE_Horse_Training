using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Data.Configurations;

public class HorseConfiguration : IEntityTypeConfiguration<Horse>
{
    public void Configure(EntityTypeBuilder<Horse> entity)
    {
        entity.HasKey(e => e.HorseId).HasName("PK__Horse__418B5D48C9CB2F6A");

        entity.ToTable("Horse");

        entity.Property(e => e.HorseId).HasColumnName("HorseID");
        entity.Property(e => e.Breed).HasMaxLength(100);
        entity.Property(e => e.Color).HasMaxLength(50);
        entity.Property(e => e.CreatedAt).HasColumnType("datetime");
        entity.Property(e => e.Description).HasMaxLength(500);
        entity.Property(e => e.Gender)
            .HasMaxLength(20)
            .IsUnicode(false);
        entity.Property(e => e.Height).HasColumnType("decimal(5, 2)");
        entity.Property(e => e.HorseName).HasMaxLength(100);
        entity.Property(e => e.ImageUrl)
            .HasMaxLength(500)
            .IsUnicode(false)
            .HasColumnName("ImageURL");
        entity.Property(e => e.HorseStatus)
            .HasMaxLength(30)
            .IsUnicode(false);
        entity.Property(e => e.LockReason).HasMaxLength(500);
        entity.Property(e => e.LockedAt).HasColumnType("datetime");
        entity.Property(e => e.LockedByUserId).HasColumnName("LockedByUserID");
        entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        entity.Property(e => e.UserId).HasColumnName("UserID");
        entity.Property(e => e.Weight).HasColumnType("decimal(6, 2)");

        entity.HasOne(d => d.LockedByUser).WithMany(p => p.HorseLockedByUsers)
            .HasForeignKey(d => d.LockedByUserId)
            .HasConstraintName("FK__Horse__LockedByU__32E0915F");

        entity.HasOne(d => d.User).WithMany(p => p.HorseUsers)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK__Horse__UserID__31EC6D26");
    }
}
