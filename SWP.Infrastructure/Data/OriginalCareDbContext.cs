using Microsoft.EntityFrameworkCore;
using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Persistence;

/// <summary>
/// Daily-care queries use only the columns in Database/Dua ngua.sql.
/// Medical, training and injury APIs share ApplicationDbContext; this context never migrates a database.
/// </summary>
public sealed class OriginalCareDbContext(DbContextOptions<OriginalCareDbContext> options)
    : ApplicationDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Ignore<InjuryCaseEvent>();
        modelBuilder.Ignore<InjuryCase>();
        modelBuilder.Ignore<TrainingPlanPhase>();

        var care = modelBuilder.Entity<CareTask>();
        // Remove the relationship first: ignoring only its CLR property lets EF
        // recreate the foreign key as a shadow column absent from the original SQL.
        foreach (var foreignKey in care.Metadata.GetForeignKeys()
                     .Where(fk => fk.Properties.Any(p => p.Name == nameof(CareTask.AssignedByUserId))).ToList())
            care.Metadata.RemoveForeignKey(foreignKey);
        care.Ignore(x => x.InjuryCaseId);
        care.Ignore(x => x.AssignedByUserId);
        care.Ignore(x => x.Version);
        // Optimistic concurrency using an existing column, without adding a version column.
        care.Property(x => x.Status).IsConcurrencyToken();

        var trainingLock = modelBuilder.Entity<TrainingLock>();
        foreach (var foreignKey in trainingLock.Metadata.GetForeignKeys()
                     .Where(fk => fk.Properties.Any(p => p.Name is nameof(TrainingLock.ReleasedByUserId)
                         or nameof(TrainingLock.ReexamRecordId))).ToList())
            trainingLock.Metadata.RemoveForeignKey(foreignKey);
        trainingLock.Ignore(x => x.InjuryCaseId);
        trainingLock.Ignore(x => x.ReleasedByUserId);
        trainingLock.Ignore(x => x.ReleaseReason);
        trainingLock.Ignore(x => x.ReexamRecordId);
        trainingLock.Property(x => x.NgayGo).IsConcurrencyToken();

        var plan = modelBuilder.Entity<TrainingPlan>();
        plan.Ignore(x => x.CreatedByUser);
        plan.Ignore(x => x.CreatedByUserId);
        plan.Ignore(x => x.Phases);

        var session = modelBuilder.Entity<TrainingSchedule>();
        session.Ignore(x => x.Phase);
        session.Ignore(x => x.PhaseId);
        session.Ignore(x => x.Surface);
        session.Ignore(x => x.ActualDistanceKm);
        session.Ignore(x => x.ActualDurationMinutes);
        session.Ignore(x => x.AvgHeartRate);
        session.Ignore(x => x.MaxHeartRate);
        session.Ignore(x => x.AvgSpeedKmh);
        session.Ignore(x => x.TrainerComment);
    }
}
