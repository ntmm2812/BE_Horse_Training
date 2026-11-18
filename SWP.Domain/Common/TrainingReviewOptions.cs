namespace SWP.Domain.Common;

/// <summary> Giá trị hợp lệ của đánh giá buổi tập do HLV gửi. </summary>
public static class TrainingReviewOptions
{
    public const string Progress = "Progress";

    public const string Stable = "Stable";

    public const string Attention = "Attention";

    public const int MinScore = 1;

    public const int MaxScore = 10;

    public const int MaxCommentLength = 500;

    public static readonly string[] Assessments = [Progress, Stable, Attention];

    public static bool TryNormalizeAssessment(string? value, out string normalized) =>
        CodeLists.TryNormalize(Assessments, value, out normalized);
}
