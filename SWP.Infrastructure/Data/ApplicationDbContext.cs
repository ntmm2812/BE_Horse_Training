using Microsoft.EntityFrameworkCore;
using SWP.Domain.Models.Entity;
using SWP.Infrastructure.Data.Configurations;

namespace SWP.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected ApplicationDbContext(DbContextOptions options) : base(options) { }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<CareTask> CareTasks { get; set; }

    public virtual DbSet<FeedingPlan> FeedingPlans { get; set; }

    public virtual DbSet<FeedingPlanDetail> FeedingPlanDetails { get; set; }

    public virtual DbSet<FinancialTransaction> FinancialTransactions { get; set; }

    public virtual DbSet<HealthMetric> HealthMetrics { get; set; }

    public virtual DbSet<Horse> Horses { get; set; }

    public virtual DbSet<HorseHealthStatus> HorseHealthStatuses { get; set; }

    public virtual DbSet<MedicalRecord> MedicalRecords { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<Race> Races { get; set; }

    public virtual DbSet<RaceRegistration> RaceRegistrations { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<RolePermission> RolePermissions { get; set; }

    public virtual DbSet<Stable> Stables { get; set; }

    public virtual DbSet<StableAssignment> StableAssignments { get; set; }

    public virtual DbSet<Supply> Supplies { get; set; }

    public virtual DbSet<TrainingPlan> TrainingPlans { get; set; }

    public virtual DbSet<TrainingPlanPhase> TrainingPlanPhases { get; set; }

    public virtual DbSet<TrainingSchedule> TrainingSchedules { get; set; }

    public virtual DbSet<TrainingLock> TrainingLocks { get; set; }

    public virtual DbSet<VaccinationSchedule> VaccinationSchedules { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuditLogConfiguration).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
