using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Data.Configurations;

public class InjuryCaseConfiguration : IEntityTypeConfiguration<InjuryCase>
{
    public void Configure(EntityTypeBuilder<InjuryCase> entity)
    {
        entity.ToTable("InjuryCase");
        entity.HasKey(e => e.InjuryCaseId);
        entity.Property(e => e.Title).HasMaxLength(150);
        entity.Property(e => e.BodyRegion).HasMaxLength(100);
        entity.Property(e => e.Side).HasMaxLength(20);
        entity.Property(e => e.Severity).HasMaxLength(20);
        entity.Property(e => e.Status).HasMaxLength(20);
        entity.Property(e => e.OwnerSummary).HasMaxLength(1000);
        entity.Property(e => e.TrainingGuidance).HasMaxLength(1000);
        entity.Property(e => e.Version).IsConcurrencyToken().HasDefaultValue(1);
        entity.HasOne(e => e.Horse).WithMany().HasForeignKey(e => e.HorseId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<User>().WithMany().HasForeignKey(e => e.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(e => new { e.HorseId, e.Status });
    }
}

public class InjuryCaseEventConfiguration : IEntityTypeConfiguration<InjuryCaseEvent>
{
    public void Configure(EntityTypeBuilder<InjuryCaseEvent> entity)
    {
        entity.ToTable("InjuryCaseEvent");
        entity.HasKey(e => e.InjuryCaseEventId);
        entity.Property(e => e.Kind).HasMaxLength(40);
        entity.HasOne<InjuryCase>().WithMany().HasForeignKey(e => e.InjuryCaseId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<User>().WithMany().HasForeignKey(e => e.ActorUserId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<MedicalRecord>().WithMany().HasForeignKey(e => e.MedicalRecordId).OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(e => new { e.InjuryCaseId, e.InjuryCaseEventId });
        entity.HasIndex(e => new { e.InjuryCaseId, e.MedicalRecordId }).IsUnique()
            .HasFilter("[MedicalRecordId] IS NOT NULL");
    }
}
