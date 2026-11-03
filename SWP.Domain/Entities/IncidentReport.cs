namespace SWP.Domain.Models.Entity;

public class IncidentReport
{
    public int ReportId { get; set; }

    public int HorseId { get; set; }

    public int GroomId { get; set; }

    public string IncidentType { get; set; } = null!;

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public DateOnly ReportedOn { get; set; }

    public DateTime CreatedAt { get; set; }

    public string Status { get; set; } = "Mới";

    public string? HandlingNote { get; set; }

    public int? HandledBy { get; set; }

    public DateTime? HandledAt { get; set; }

    public virtual Horse Horse { get; set; } = null!;

    public virtual User Groom { get; set; } = null!;

    public virtual User? Handler { get; set; }
}
