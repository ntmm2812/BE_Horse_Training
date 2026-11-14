using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Data.Configurations;

public class TrainingSessionReviewConfiguration : IEntityTypeConfiguration<TrainingSessionReview>
{
    public void Configure(EntityTypeBuilder<TrainingSessionReview> entity)
    {
        entity.ToTable("TrainingSessionReview");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Assessment).HasMaxLength(20);
        entity.Property(x => x.Comment).HasMaxLength(500);
        entity.HasOne(x => x.Schedule).WithOne(s => s.Review).HasForeignKey<TrainingSessionReview>(x => x.ScheduleId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(x => x.ReviewedByUser).WithMany().HasForeignKey(x => x.ReviewedByUserId).OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(x => x.ScheduleId).IsUnique();
    }
}
