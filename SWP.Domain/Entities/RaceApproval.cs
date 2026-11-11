namespace SWP.Domain.Models.Entity;

// A proposal is separate from an actual race entry. Only manager approval creates the entry.
public class RaceApproval
{
    public int Id { get; set; }
    public int HorseId { get; set; }
    public Horse Horse { get; set; } = null!;
    public int RaceId { get; set; }
    public Race Race { get; set; } = null!;
    public int TrainerId { get; set; }
    public User Trainer { get; set; } = null!;
    public string Status { get; set; } = "Draft";
    public string Note { get; set; } = "";
    public string? RejectedBy { get; set; }
    public string RejectReason { get; set; } = "";
    public bool TrainerSeen { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string HistoryJson { get; set; } = "[]";
    public string Version { get; set; } = Guid.NewGuid().ToString();
}
