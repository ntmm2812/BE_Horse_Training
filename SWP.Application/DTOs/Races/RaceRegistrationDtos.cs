using System.ComponentModel.DataAnnotations;

namespace SWP.Application.DTOs.Races;

public class RaceRegistrationDto
{
    public int RegistrationId { get; set; }

    public int RaceId { get; set; }

    public string? RaceName { get; set; }

    public string? RaceCode { get; set; }

    public DateTime? RaceDate { get; set; }

    public string? Location { get; set; }

    public int? DistanceMeters { get; set; }

    public int HorseId { get; set; }

    public string HorseName { get; set; } = null!;

    public string? HorseCode { get; set; }

    public int? OwnerUserId { get; set; }

    public string? OwnerName { get; set; }

    public int? RegisteredByUserId { get; set; }

    public string? RegisteredByName { get; set; }

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
}

public class RegisterHorseForRaceDto
{
    [Required(ErrorMessage = "Mã ngựa là bắt buộc.")]
    public int HorseId { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class UpdateRaceRegistrationStatusDto
{
    [Required]
    public string Status { get; set; } = null!;

    [Range(1, 999)]
    public int? HorseNumber { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class RecordRaceResultDto
{
    [Required(ErrorMessage = "Registration ID là bắt buộc.")]
    public int RegistrationId { get; set; }

    [Range(1, 100, ErrorMessage = "Vị trí về đích từ 1 đến 100.")]
    public int? FinishPosition { get; set; }

    [Range(0, 100000, ErrorMessage = "Thời gian về đích không hợp lệ.")]
    public decimal? FinishTimeSeconds { get; set; }

    [Range(0, 10000000000, ErrorMessage = "Tiền thưởng không hợp lệ.")]
    public decimal? PrizeMoney { get; set; }

    [MaxLength(30)]
    public string? ResultStatus { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class BatchRecordRaceResultsDto
{
    [Required]
    public List<RecordRaceResultDto> Results { get; set; } = new();
}
