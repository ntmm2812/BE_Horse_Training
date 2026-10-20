using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Data.Configurations;

public class MedicalRecordConfiguration : IEntityTypeConfiguration<MedicalRecord>
{
    public void Configure(EntityTypeBuilder<MedicalRecord> entity)
    {
        entity.HasKey(e => e.MedicalRecordId).HasName("PK__MedicalR__4411BBC27F4E0132");

        entity.ToTable("MedicalRecord");

        entity.Property(e => e.MedicalRecordId).HasColumnName("MedicalRecordID");
        entity.Property(e => e.Cost).HasColumnType("decimal(18, 2)");
        entity.Property(e => e.CreatedAt).HasColumnType("datetime");
        entity.Property(e => e.Diagnosis).HasMaxLength(500);
        entity.Property(e => e.Dosage).HasMaxLength(200);
        entity.Property(e => e.HorseId).HasColumnName("HorseID");
        entity.Property(e => e.Medication).HasMaxLength(500);
        entity.Property(e => e.Notes).HasMaxLength(1000);
        entity.Property(e => e.RecordDate).HasColumnType("datetime");
        entity.Property(e => e.RecordType)
            .HasMaxLength(50)
            .IsUnicode(false);
        entity.Property(e => e.Status)
            .HasMaxLength(30)
            .IsUnicode(false);
        entity.Property(e => e.Symptoms).HasMaxLength(1000);
        entity.Property(e => e.Treatment).HasMaxLength(1000);
        entity.Property(e => e.ViTriChanThuong).HasMaxLength(255);

        entity.HasOne(d => d.CreateByNavigation).WithMany(p => p.MedicalRecords)
            .HasForeignKey(d => d.CreateBy)
            .HasConstraintName("FK__MedicalRe__Creat__403A8C7D");

        entity.HasOne(d => d.Horse).WithMany(p => p.MedicalRecords)
            .HasForeignKey(d => d.HorseId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK__MedicalRe__Horse__3F466844");
    }
}
