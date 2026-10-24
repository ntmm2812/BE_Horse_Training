using System;
using System.Collections.Generic;

namespace SWP.Domain.Models.Entity;

public class Horse
{
    public int HorseId { get; set; }

    public string HorseName { get; set; } = null!;

    public string? Breed { get; set; }

    public string? Gender { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? Color { get; set; }

    public decimal? Weight { get; set; }

    public decimal? Height { get; set; }

    public string HorseStatus { get; set; } = null!;

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? LockReason { get; set; }

    public int? LockedByUserId { get; set; }

    public DateTime? LockedAt { get; set; }

    public int UserId { get; set; }

    public virtual ICollection<CareTask> CareTasks { get; set; } = new List<CareTask>();

    public virtual ICollection<FeedingPlan> FeedingPlans { get; set; } = new List<FeedingPlan>();

    public virtual HorseHealthStatus? HealthStatusRecord { get; set; }

    public virtual ICollection<HealthMetric> HealthMetrics { get; set; } = new List<HealthMetric>();

    public virtual User? LockedByUser { get; set; }

    public virtual ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();

    public virtual ICollection<RaceRegistration> RaceRegistrations { get; set; } = new List<RaceRegistration>();

    public virtual ICollection<StableAssignment> StableAssignments { get; set; } = new List<StableAssignment>();

    public virtual ICollection<TrainingPlan> TrainingPlans { get; set; } = new List<TrainingPlan>();

    public virtual ICollection<TrainingLock> TrainingLocks { get; set; } = new List<TrainingLock>();

    public virtual ICollection<VaccinationSchedule> VaccinationSchedules { get; set; } = new List<VaccinationSchedule>();

    public virtual User User { get; set; } = null!;
}
