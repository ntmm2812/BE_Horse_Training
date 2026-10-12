using System;
using System.Collections.Generic;

namespace SWP.Domain.Models.Entity;

public class FeedingPlan
{
    public int FeedingPlanId { get; set; }

    public int HorseId { get; set; }

    public int UserId { get; set; }

    public string PlanName { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public int? DailyMealCount { get; set; }

    public string Status { get; set; } = null!;

    public string? SpecialInstructions { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<FeedingPlanDetail> FeedingPlanDetails { get; set; } = new List<FeedingPlanDetail>();

    public virtual Horse Horse { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
