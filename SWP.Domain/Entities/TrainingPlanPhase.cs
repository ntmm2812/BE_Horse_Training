using System;
using System.Collections.Generic;

namespace SWP.Domain.Models.Entity;

/// <summary> Một giai đoạn của giáo án: khoảng ngày, cường độ tối đa và khối lượng mỗi buổi. </summary>
public class TrainingPlanPhase
{
    public int PhaseId { get; set; }

    public int PlanId { get; set; }

    public int PhaseOrder { get; set; }

    public string PhaseName { get; set; } = null!;

    public string? Goal { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public string IntensityLevel { get; set; } = null!;

    public int SessionsPerWeek { get; set; }

    public decimal TargetDistanceKm { get; set; }

    public int SessionDurationMinutes { get; set; }

    public string? Surface { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual TrainingPlan Plan { get; set; } = null!;

    public virtual ICollection<TrainingSchedule> TrainingSchedules { get; set; } = new List<TrainingSchedule>();
}
