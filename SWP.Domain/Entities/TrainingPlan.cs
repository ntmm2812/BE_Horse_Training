using System;
using System.Collections.Generic;

namespace SWP.Domain.Models.Entity;

public class TrainingPlan
{
    public int PlanId { get; set; }

    public string PlanName { get; set; } = null!;

    public string? Objective { get; set; }

    public string? TrainingType { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? IntensityLevel { get; set; }

    public string PlanStatus { get; set; } = null!;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int HorseId { get; set; }

    public int? CreatedByUserId { get; set; }

    public virtual Horse Horse { get; set; } = null!;

    public virtual User? CreatedByUser { get; set; }

    public virtual ICollection<TrainingPlanPhase> Phases { get; set; } = new List<TrainingPlanPhase>();

    public virtual ICollection<TrainingSchedule> TrainingSchedules { get; set; } = new List<TrainingSchedule>();
}
