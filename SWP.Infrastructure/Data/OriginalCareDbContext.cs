using Microsoft.EntityFrameworkCore;
using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Persistence;

/// <summary>
/// Medical and daily-care APIs use only the columns in Database/Dua ngua.sql.
/// The extended training/injury model remains separate; this context never migrates a database.
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
        care.Ignore(x => x.InjuryCaseId);
        care.Ignore(x => x.AssignedByUserId);
        care.Ignore(x => x.Version);
        // Optimistic concurrency using an existing column, without adding a version column.
        care.Property(x => x.Status).IsConcurrencyToken();

        var trainingLock = modelBuilder.Entity<TrainingLock>();
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
