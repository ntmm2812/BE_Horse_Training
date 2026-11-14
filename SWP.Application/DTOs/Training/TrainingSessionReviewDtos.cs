namespace SWP.Application.DTOs.Training;

public class SubmitSessionReviewRequestDto
{
    /// <summary> Điểm phong độ 1–10. </summary>
    public int? Score { get; set; }

    /// <summary> Progress | Stable | Attention. </summary>
    public string? Assessment { get; set; }

    /// <summary> Bắt buộc, tối đa 500 ký tự. </summary>
    public string? Comment { get; set; }
}

public class SessionReviewDto
{
    public int Score { get; set; }

    public string Assessment { get; set; } = null!;

    public string Comment { get; set; } = null!;

    public string? ReviewedByName { get; set; }

    public DateTime ReviewedAt { get; set; }

    /// <summary> null: chưa gửi chủ ngựa. </summary>
    public DateTime? SentToOwnerAt { get; set; }
}

/// <summary> Đánh giá đã gửi, dành cho chủ ngựa (không kèm báo cáo của nhân viên chăm sóc). </summary>
public class OwnerSessionReviewDto
{
    public int SessionId { get; set; }

    public int HorseId { get; set; }

    public string? HorseName { get; set; }

    public DateOnly TrainingDate { get; set; }

    public string? SessionTitle { get; set; }

    public int Score { get; set; }

    public string Assessment { get; set; } = null!;

    public string Comment { get; set; } = null!;

    public string? TrainerName { get; set; }

    public DateTime SentAt { get; set; }
}
