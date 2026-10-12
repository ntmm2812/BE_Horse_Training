using System;
using System.Collections.Generic;

namespace SWP.Domain.Models.Entity;

public class FeedingPlanDetail
{
    public int FeedingPlanDetailId { get; set; }

    public int FeedingPlanId { get; set; }

    public string? MealName { get; set; }

    public string? FeedName { get; set; }

    public decimal Quantity { get; set; }

    public string? Unit { get; set; }

    public TimeOnly? FeedingTime { get; set; }

    public decimal? Calories { get; set; }

    public string? Instructions { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual FeedingPlan FeedingPlan { get; set; } = null!;
}
