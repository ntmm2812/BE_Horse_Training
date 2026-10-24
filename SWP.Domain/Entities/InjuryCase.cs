namespace SWP.Domain.Models.Entity;

/// <summary>Một đợt chấn thương, có thể tồn tại song song với các đợt khác của cùng ngựa.</summary>
public class InjuryCase
{
    public int InjuryCaseId { get; set; }
    public int HorseId { get; set; }
    public int CreatedByUserId { get; set; }
    public string Title { get; set; } = null!;
    public string BodyRegion { get; set; } = null!;
    public string Side { get; set; } = null!;
    public string Severity { get; set; } = null!;
    public string Status { get; set; } = "Open";
    public string OwnerSummary { get; set; } = null!;
    public string TrainingGuidance { get; set; } = null!;
    public DateTime? ReviewDueAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public int Version { get; set; } = 1;
    public Horse Horse { get; set; } = null!;
}
