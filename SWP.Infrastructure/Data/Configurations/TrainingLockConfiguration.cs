using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Data.Configurations;

public class TrainingLockConfiguration : IEntityTypeConfiguration<TrainingLock>
{
    public void Configure(EntityTypeBuilder<TrainingLock> entity)
    {
        entity.HasKey(e => e.LockId);
        entity.ToTable("TrainingLock");

        entity.Property(e => e.LockId).HasColumnName("LockID");
        entity.Property(e => e.HorseId).HasColumnName("HorseID");
        entity.Property(e => e.VetId).HasColumnName("VetID");
        entity.Property(e => e.LyDo).HasMaxLength(200).IsRequired();
        entity.Property(e => e.NgayDat).HasColumnType("date");
        entity.Property(e => e.NgayGo).HasColumnType("date");

        entity.HasOne(d => d.Horse)
            .WithMany(p => p.TrainingLocks)
            .HasForeignKey(d => d.HorseId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(d => d.Vet)
            .WithMany(p => p.TrainingLocks)
            .HasForeignKey(d => d.VetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
