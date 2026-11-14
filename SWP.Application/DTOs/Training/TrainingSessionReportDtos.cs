namespace SWP.Application.DTOs.Training;

public class SubmitSessionReportRequestDto
{
    /// <summary> Completed | Abandoned | NotTrained. </summary>
    public string? Result { get; set; }

    /// <summary> 0–50 km; bỏ trống khi Result = NotTrained. </summary>
    public decimal? ActualDistanceKm { get; set; }

    /// <summary> Good | Normal | Tired | Abnormal. </summary>
    public string? HorseReaction { get; set; }

    /// <summary> Tối đa 300 ký tự. </summary>
    public string? Note { get; set; }
}

public class SessionReportDto
{
    public string Result { get; set; } = null!;

    public decimal? ActualDistanceKm { get; set; }

    public string HorseReaction { get; set; } = null!;

    public string Note { get; set; } = "";

    public string? ReportedByName { get; set; }

    public DateTime ReportedAt { get; set; }
}
