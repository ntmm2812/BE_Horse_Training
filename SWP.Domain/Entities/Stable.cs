using System;
using System.Collections.Generic;

namespace SWP.Domain.Models.Entity;

public class Stable
{
    public int StableId { get; set; }

    public string StableName { get; set; } = null!;

    public string? Location { get; set; }

    public int? Capacity { get; set; }

    public int CurrentOccupancy { get; set; }

    public string? StableType { get; set; }

    public string? AttachmentUrl { get; set; }

    public string Status { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<StableAssignment> StableAssignments { get; set; } = new List<StableAssignment>();
}
