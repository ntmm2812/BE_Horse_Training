using System.ComponentModel.DataAnnotations;

namespace SWP.Application.DTOs.Races;

public class CreateRaceDto
{
    [Required(ErrorMessage = "Tên giải đua không được để trống.")]
    [MaxLength(150, ErrorMessage = "Tên giải đua không vượt quá 150 ký tự.")]
    public string RaceName { get; set; } = null!;

    [MaxLength(50, ErrorMessage = "Mã giải đua không vượt quá 50 ký tự.")]
    public string? RaceCode { get; set; }

    [MaxLength(50)]
    public string? RaceType { get; set; }

    [MaxLength(255)]
    public string? Location { get; set; }

    [Required(ErrorMessage = "Ngày diễn ra giải đua là bắt buộc.")]
    public DateTime RaceDate { get; set; }

    public DateTime? RegistrationDeadline { get; set; }

    [Range(100, 100000, ErrorMessage = "Cự ly đua phải từ 100m đến 100,000m.")]
    public int? DistanceMeters { get; set; }

    [Range(2, 100, ErrorMessage = "Số lượng tham gia tối đa từ 2 đến 100.")]
    public int? MaxParticipants { get; set; }

    [Range(0, 1000000000, ErrorMessage = "Lệ phí tham gia không hợp lệ.")]
    public decimal? EntryFee { get; set; }

    [Range(0, 10000000000, ErrorMessage = "Tổng giải thưởng không hợp lệ.")]
    public decimal? PrizePool { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }
}

public class UpdateRaceDto
{
    [Required(ErrorMessage = "Tên giải đua không được để trống.")]
    [MaxLength(150)]
    public string RaceName { get; set; } = null!;

    [MaxLength(50)]
    public string? RaceType { get; set; }

    [MaxLength(255)]
    public string? Location { get; set; }

    public DateTime? RaceDate { get; set; }

    public DateTime? RegistrationDeadline { get; set; }

    [Range(100, 100000)]
    public int? DistanceMeters { get; set; }

    [Range(2, 100)]
    public int? MaxParticipants { get; set; }

    [Range(0, 1000000000)]
    public decimal? EntryFee { get; set; }

    [Range(0, 10000000000)]
    public decimal? PrizePool { get; set; }

    public string? Status { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }
}
