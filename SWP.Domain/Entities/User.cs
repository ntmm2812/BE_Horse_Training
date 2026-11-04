using System;
using System.Collections.Generic;

namespace SWP.Domain.Models.Entity;

public class User
{
    public int UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string? PasswordHash { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public string? Address { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? GoogleId { get; set; }

    public int RoleId { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<CareTask> CareTasks { get; set; } = new List<CareTask>();

    public virtual ICollection<IncidentReport> IncidentReports { get; set; } = new List<IncidentReport>();

    public virtual ICollection<FeedingPlan> FeedingPlans { get; set; } = new List<FeedingPlan>();

    public virtual ICollection<FinancialTransaction> FinancialTransactions { get; set; } = new List<FinancialTransaction>();

    public virtual ICollection<Horse> HorseLockedByUsers { get; set; } = new List<Horse>();

    public virtual ICollection<Horse> HorseUsers { get; set; } = new List<Horse>();

    public virtual ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();

    public virtual ICollection<RaceRegistration> RaceRegistrations { get; set; } = new List<RaceRegistration>();

    public virtual ICollection<Race> Races { get; set; } = new List<Race>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<Supply> Supplies { get; set; } = new List<Supply>();

    public virtual ICollection<TrainingLock> TrainingLocks { get; set; } = new List<TrainingLock>();

    public virtual ICollection<TrainingSchedule> TrainingSchedules { get; set; } = new List<TrainingSchedule>();
}
