using System;
using System.Collections.Generic;

namespace SWP.Domain.Models.Entity;

public class StableAssignment
{
    public int AssignmentId { get; set; }

    public int HorseId { get; set; }

    public int StableId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string Status { get; set; } = null!;

    public string? Notes { get; set; }

    public virtual Horse Horse { get; set; } = null!;

    public virtual Stable Stable { get; set; } = null!;
}
