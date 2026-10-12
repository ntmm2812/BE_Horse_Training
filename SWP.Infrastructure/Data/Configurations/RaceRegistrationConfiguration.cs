using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Data.Configurations;

public class RaceRegistrationConfiguration : IEntityTypeConfiguration<RaceRegistration>
{
    public void Configure(EntityTypeBuilder<RaceRegistration> entity)
    {
        entity.HasKey(e => e.RegistrationId).HasName("PK__RaceRegi__6EF588307406F348");

        entity.ToTable("RaceRegistration");

        entity.Property(e => e.RegistrationId).HasColumnName("RegistrationID");
        entity.Property(e => e.CreatedAt).HasColumnType("datetime");
        entity.Property(e => e.FinishTimeSeconds).HasColumnType("decimal(10, 3)");
        entity.Property(e => e.HorseId).HasColumnName("HorseID");
        entity.Property(e => e.Notes).HasMaxLength(500);
        entity.Property(e => e.PrizeMoney).HasColumnType("decimal(18, 2)");
        entity.Property(e => e.RaceId).HasColumnName("RaceID");
        entity.Property(e => e.RegistrationDate).HasColumnType("datetime");
        entity.Property(e => e.RegistrationStatus)
            .HasMaxLength(30)
            .IsUnicode(false);
        entity.Property(e => e.ResultStatus)
            .HasMaxLength(30)
            .IsUnicode(false);
        entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.HasOne(d => d.Horse).WithMany(p => p.RaceRegistrations)
            .HasForeignKey(d => d.HorseId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK__RaceRegis__Horse__5AEE82B9");

        entity.HasOne(d => d.Race).WithMany(p => p.RaceRegistrations)
            .HasForeignKey(d => d.RaceId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK__RaceRegis__RaceI__59FA5E80");

        entity.HasOne(d => d.User).WithMany(p => p.RaceRegistrations)
            .HasForeignKey(d => d.UserId)
            .HasConstraintName("FK__RaceRegis__UserI__5BE2A6F2");
    }
}
