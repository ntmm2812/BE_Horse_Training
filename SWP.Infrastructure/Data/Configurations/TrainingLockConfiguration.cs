using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Data.Configurations;

public class TrainingLockConfiguration : IEntityTypeConfiguration<TrainingLock>
{
    public void Configure(EntityTypeBuilder<TrainingLock> entity)
    {
        entity.ToTable("TrainingLock");
        entity.HasKey(e => e.LockId);
        entity.Property(e => e.LockId).HasColumnName("LockID").ValueGeneratedOnAdd();
        entity.Property(e => e.HorseId).HasColumnName("HorseID");
        entity.Property(e => e.VetId).HasColumnName("VetID");
        entity.Property(e => e.Reason).HasColumnName("LyDo").HasMaxLength(200);
        entity.Property(e => e.PlacedOn).HasColumnName("NgayDat").HasColumnType("date");
        entity.Property(e => e.ReleasedOn).HasColumnName("NgayGo").HasColumnType("date");
        entity.Property(e => e.CreatedAt).HasColumnType("datetime");

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
