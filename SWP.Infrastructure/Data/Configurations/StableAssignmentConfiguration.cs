using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Data.Configurations;

public class StableAssignmentConfiguration : IEntityTypeConfiguration<StableAssignment>
{
    public void Configure(EntityTypeBuilder<StableAssignment> entity)
    {
        entity.HasKey(e => e.AssignmentId).HasName("PK__StableAs__32499E577227A6A3");

        entity.ToTable("StableAssignment");

        entity.Property(e => e.AssignmentId).HasColumnName("AssignmentID");
        entity.Property(e => e.EndDate).HasColumnType("datetime");
        entity.Property(e => e.HorseId).HasColumnName("HorseID");
        entity.Property(e => e.Notes).HasMaxLength(500);
        entity.Property(e => e.StableId).HasColumnName("StableID");
        entity.Property(e => e.StartDate).HasColumnType("datetime");
        entity.Property(e => e.Status)
            .HasMaxLength(30)
            .IsUnicode(false);

        entity.HasOne(d => d.Horse).WithMany(p => p.StableAssignments)
            .HasForeignKey(d => d.HorseId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK__StableAss__Horse__44FF419A");

        entity.HasOne(d => d.Stable).WithMany(p => p.StableAssignments)
            .HasForeignKey(d => d.StableId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK__StableAss__Stabl__45F365D3");
    }
}
