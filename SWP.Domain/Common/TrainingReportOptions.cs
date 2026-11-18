namespace SWP.Domain.Common;

/// <summary> Giá trị hợp lệ của báo cáo buổi tập do nhân viên chăm sóc gửi. </summary>
public static class TrainingReportOptions
{
    public const string Completed = "Completed";

    public const string Abandoned = "Abandoned";

    public const string NotTrained = "NotTrained";

    public const decimal MaxDistanceKm = 50;

    public const int MaxNoteLength = 300;

    public static readonly string[] Results = [Completed, Abandoned, NotTrained];

    public static readonly string[] Reactions = ["Good", "Normal", "Tired", "Abnormal"];

    public static bool TryNormalizeResult(string? value, out string normalized) =>
        CodeLists.TryNormalize(Results, value, out normalized);

    public static bool TryNormalizeReaction(string? value, out string normalized) =>
        CodeLists.TryNormalize(Reactions, value, out normalized);
}
