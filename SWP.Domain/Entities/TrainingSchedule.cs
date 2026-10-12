using System;
using System.Collections.Generic;

namespace SWP.Domain.Models.Entity;

public class TrainingSchedule
{
    public int ScheduleId { get; set; }

    public string? SessionTitle { get; set; }

    public DateTime TrainingDate { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public string? TrainingType { get; set; }

    public int? DurationMinutes { get; set; }

    public decimal? DistanceKm { get; set; }

    public decimal? PerformanceScore { get; set; }

    public string? Result { get; set; }

    public string Status { get; set; } = null!;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? TrialVideoUrl { get; set; }

    public int PlanId { get; set; }

    public int? AssignedToUserId { get; set; }

    public virtual User? AssignedToUser { get; set; }

    public virtual TrainingPlan Plan { get; set; } = null!;
}
