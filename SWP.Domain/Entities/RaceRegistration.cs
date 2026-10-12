using System;
using System.Collections.Generic;

namespace SWP.Domain.Models.Entity;

public class RaceRegistration
{
    public int RegistrationId { get; set; }

    public int RaceId { get; set; }

    public int HorseId { get; set; }

    public int? UserId { get; set; }

    public DateTime RegistrationDate { get; set; }

    public string RegistrationStatus { get; set; } = null!;

    public int? HorseNumber { get; set; }

    public int? FinishPosition { get; set; }

    public decimal? FinishTimeSeconds { get; set; }

    public decimal? PrizeMoney { get; set; }

    public string? ResultStatus { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Horse Horse { get; set; } = null!;

    public virtual Race Race { get; set; } = null!;

    public virtual User? User { get; set; }
}
