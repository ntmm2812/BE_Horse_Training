using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Data.Configurations;

public class CareTaskConfiguration : IEntityTypeConfiguration<CareTask>
{
    public void Configure(EntityTypeBuilder<CareTask> entity)
    {
        entity.HasKey(e => e.CareTaskId).HasName("PK__CareTask__C24D790B98D9A741");

        entity.ToTable("CareTask");

        entity.Property(e => e.CareTaskId).HasColumnName("CareTaskID");
        entity.Property(e => e.CompletedAt).HasColumnType("datetime");
        entity.Property(e => e.CompletionNotes).HasMaxLength(1000);
        entity.Property(e => e.CreatedAt).HasColumnType("datetime");
        entity.Property(e => e.Description).HasMaxLength(1000);
        entity.Property(e => e.HorseId).HasColumnName("HorseID");
        entity.Property(e => e.Priority)
            .HasMaxLength(20)
            .IsUnicode(false);
        entity.Property(e => e.ScheduledAt).HasColumnType("datetime");
        entity.Property(e => e.Status)
            .HasMaxLength(30)
            .IsUnicode(false);
        entity.Property(e => e.TaskName).HasMaxLength(150);
        entity.Property(e => e.TaskType)
            .HasMaxLength(50)
            .IsUnicode(false);
        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.HasOne(d => d.Horse).WithMany(p => p.CareTasks)
            .HasForeignKey(d => d.HorseId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK__CareTask__HorseI__48CFD27E");

        entity.HasOne(d => d.User).WithMany(p => p.CareTasks)
            .HasForeignKey(d => d.UserId)
            .HasConstraintName("FK__CareTask__UserID__49C3F6B7");
    }
}
