using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Data.Configurations;

public class StableConfiguration : IEntityTypeConfiguration<Stable>
{
    public void Configure(EntityTypeBuilder<Stable> entity)
    {
        entity.HasKey(e => e.StableId).HasName("PK__Stable__1A0CAEC5BC4B7A19");

        entity.ToTable("Stable");

        entity.Property(e => e.StableId).HasColumnName("StableID");
        entity.Property(e => e.AttachmentUrl).HasMaxLength(500);
        entity.Property(e => e.CreatedAt).HasColumnType("datetime");
        entity.Property(e => e.Description).HasMaxLength(500);
        entity.Property(e => e.Location).HasMaxLength(255);
        entity.Property(e => e.StableName).HasMaxLength(100);
        entity.Property(e => e.StableType)
            .HasMaxLength(50)
            .IsUnicode(false);
        entity.Property(e => e.Status)
            .HasMaxLength(30)
            .IsUnicode(false);
        entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
    }
}
