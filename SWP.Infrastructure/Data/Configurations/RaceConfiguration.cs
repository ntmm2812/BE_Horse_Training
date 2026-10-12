using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Data.Configurations;

public class RaceConfiguration : IEntityTypeConfiguration<Race>
{
    public void Configure(EntityTypeBuilder<Race> entity)
    {
        entity.HasKey(e => e.RaceId).HasName("PK__Race__05FBD6D4526D5988");

        entity.ToTable("Race");

        entity.HasIndex(e => e.RaceCode, "UQ__Race__54465BEB8F629A98").IsUnique();

        entity.Property(e => e.RaceId).HasColumnName("RaceID");
        entity.Property(e => e.CreatedAt).HasColumnType("datetime");
        entity.Property(e => e.Description).HasMaxLength(1000);
        entity.Property(e => e.EntryFee).HasColumnType("decimal(18, 2)");
        entity.Property(e => e.Location).HasMaxLength(255);
        entity.Property(e => e.PrizePool).HasColumnType("decimal(18, 2)");
        entity.Property(e => e.RaceCode)
            .HasMaxLength(50)
            .IsUnicode(false);
        entity.Property(e => e.RaceDate).HasColumnType("datetime");
        entity.Property(e => e.RaceName).HasMaxLength(150);
        entity.Property(e => e.RaceType)
            .HasMaxLength(50)
            .IsUnicode(false);
        entity.Property(e => e.RegistrationDeadline).HasColumnType("datetime");
        entity.Property(e => e.Status)
            .HasMaxLength(30)
            .IsUnicode(false);
        entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.HasOne(d => d.User).WithMany(p => p.Races)
            .HasForeignKey(d => d.UserId)
            .HasConstraintName("FK__Race__UserID__571DF1D5");
    }
}
