namespace SWP.Domain.Common;

public static class CareTaskStatuses
{
    public const string Pending = "Pending";

    public const string Completed = "Completed";

    public static bool IsCompleted(string? status, DateTime? completedAt) =>
        completedAt.HasValue
        || string.Equals(status, Completed, StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, "Done", StringComparison.OrdinalIgnoreCase);
}

public static class IncidentTypes
{
    public const int DescriptionMaxLength = 300;

    public const int ImageMaxLength = 8_000_000;

    public static readonly string[] All =
    [
        "Vết thương",
        "Khập khiễng",
        "Bỏ ăn",
        "Chuồng/thiết bị hỏng",
        "Khác",
    ];

    public static bool TryNormalize(string? value, out string normalized)
    {
        var trimmed = (value ?? string.Empty).Trim();
        var match = All.FirstOrDefault(type => string.Equals(type, trimmed, StringComparison.Ordinal));
        normalized = match ?? string.Empty;
        return match is not null;
    }
}

public static class FeedingPlanStatuses
{
    public static bool IsCurrent(string? status)
    {
        var value = (status ?? string.Empty).Trim();
        return value.Equals("Active", StringComparison.OrdinalIgnoreCase)
            || value.Equals("Approved", StringComparison.OrdinalIgnoreCase)
            || value.Equals("Đã duyệt", StringComparison.OrdinalIgnoreCase);
    }
}
