namespace SWP.Application.DTOs.Races;

public class RaceEligibleHorseDto
{
    public int HorseId { get; set; }

    public string HorseName { get; set; } = null!;

    public string? HorseCode { get; set; }

    public string? Breed { get; set; }

    public int? AgeYears { get; set; }

    public string? CurrentHealthStatus { get; set; }

    public bool IsEligible { get; set; }

    public string? IneligibilityReason { get; set; }
}

public class HorsePerformanceReportDto
{
    public int HorseId { get; set; }

    public string HorseName { get; set; } = null!;

    public string? HorseCode { get; set; }

    public int TotalRaces { get; set; }

    public int TotalWins { get; set; } // Hạng 1

    public int TotalPodiums { get; set; } // Top 3

    public decimal WinRatePercentage { get; set; }

    public decimal PodiumRatePercentage { get; set; }

    public decimal TotalPrizeMoney { get; set; }

    public decimal? BestFinishTimeSeconds { get; set; }

    public List<RaceRegistrationDto> RaceHistory { get; set; } = new();
}

public class OwnerRaceSummaryDto
{
    public int OwnerUserId { get; set; }

    public int TotalHorsesParticipated { get; set; }

    public int TotalRacesEntered { get; set; }

    public int TotalWins { get; set; }

    public int TotalPodiums { get; set; }

    public decimal TotalPrizeMoneyEarned { get; set; }

    public List<HorsePerformanceReportDto> HorsesPerformance { get; set; } = new();
}

public class ClubRaceSummaryReportDto
{
    public int TotalRacesOrganized { get; set; }

    public int CompletedRaces { get; set; }

    public int UpcomingRaces { get; set; }

    public int TotalRegistrations { get; set; }

    public decimal TotalPrizePoolDistributed { get; set; }

    public decimal TotalEntryFeesCollected { get; set; }

    public List<RaceDto> RecentRaces { get; set; } = new();
}
