using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Data.Configurations;

public class FinancialTransactionConfiguration : IEntityTypeConfiguration<FinancialTransaction>
{
    public void Configure(EntityTypeBuilder<FinancialTransaction> entity)
    {
        entity.HasKey(e => e.TransactionId).HasName("PK__Financia__55433A4B5EF1E70F");

        entity.ToTable("FinancialTransaction");

        entity.HasIndex(e => e.TransactionCode, "UQ__Financia__D85E7026B3F4DAE7").IsUnique();

        entity.Property(e => e.TransactionId).HasColumnName("TransactionID");
        entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
        entity.Property(e => e.Category)
            .HasMaxLength(50)
            .IsUnicode(false);
        entity.Property(e => e.CreatedAt).HasColumnType("datetime");
        entity.Property(e => e.Currency)
            .HasMaxLength(10)
            .IsUnicode(false);
        entity.Property(e => e.Date).HasColumnType("datetime");
        entity.Property(e => e.Description).HasMaxLength(500);
        entity.Property(e => e.PaymentMethod)
            .HasMaxLength(30)
            .IsUnicode(false);
        entity.Property(e => e.Status)
            .HasMaxLength(30)
            .IsUnicode(false);
        entity.Property(e => e.TransactionCode)
            .HasMaxLength(50)
            .IsUnicode(false);
        entity.Property(e => e.TransactionType)
            .HasMaxLength(30)
            .IsUnicode(false);
        entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.HasOne(d => d.User).WithMany(p => p.FinancialTransactions)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK__Financial__UserI__5FB337D6");
    }
}
