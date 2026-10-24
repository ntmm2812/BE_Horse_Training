using System;
using System.Collections.Generic;

namespace SWP.Domain.Models.Entity;

public class CareTask
{
    public int CareTaskId { get; set; }

    // NULL: công việc chăm sóc thông thường từ trước, không thuộc luồng chấn thương.
    public int? InjuryCaseId { get; set; }
    public int? AssignedByUserId { get; set; }
    public int Version { get; set; } = 1;

    public int HorseId { get; set; }

    public string TaskName { get; set; } = null!;

    public string? TaskType { get; set; }

    public string? Description { get; set; }

    public DateTime? ScheduledAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? Priority { get; set; }

    public string Status { get; set; } = null!;

    public string? CompletionNotes { get; set; }

    public int? UserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Horse Horse { get; set; } = null!;

    public virtual User? User { get; set; }
}
