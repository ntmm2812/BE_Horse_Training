using System;
using System.Collections.Generic;

namespace SWP.Domain.Models.Entity;

public class Race
{
    public int RaceId { get; set; }

    public string RaceName { get; set; } = null!;

    public string? RaceCode { get; set; }

    public string? RaceType { get; set; }

    public string? Location { get; set; }

    public DateTime? RaceDate { get; set; }

    public DateTime? RegistrationDeadline { get; set; }

    public int? DistanceMeters { get; set; }

    public int? MaxParticipants { get; set; }

    public decimal? EntryFee { get; set; }

    public decimal? PrizePool { get; set; }

    public string Status { get; set; } = null!;

    public string? Description { get; set; }

    public int? UserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<RaceRegistration> RaceRegistrations { get; set; } = new List<RaceRegistration>();

    public virtual User? User { get; set; }
}
