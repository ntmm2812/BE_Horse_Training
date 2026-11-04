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

    public static readonly string[] Health =
    [
        "Vết thương",
        "Khập khiễng",
        "Bỏ ăn",
    ];

    // Sự cố chuồng trại / thiết bị — Club Manager xử lý, không hiện cho bác sĩ thú y
    public static readonly string[] Facility =
    [
        "Chuồng/thiết bị hỏng",
    ];

    public static bool IsHealth(string? incidentType) =>
        Health.Contains((incidentType ?? string.Empty).Trim(), StringComparer.Ordinal);

    public static bool IsFacility(string? incidentType) =>
        Facility.Contains((incidentType ?? string.Empty).Trim(), StringComparer.Ordinal);

    public static bool TryNormalize(string? value, out string normalized)
    {
        var trimmed = (value ?? string.Empty).Trim();
        var match = All.FirstOrDefault(type => string.Equals(type, trimmed, StringComparison.Ordinal));
        normalized = match ?? string.Empty;
        return match is not null;
    }
}

public static class IncidentStatuses
{
    public const string New = "Mới";

    public const string InProgress = "Đang xử lý";

    public const string Done = "Đã xử lý";

    public static string NormalizeOrDefault(string? status)
    {
        var value = (status ?? string.Empty).Trim();
        if (string.Equals(value, New, StringComparison.Ordinal)
            || string.Equals(value, InProgress, StringComparison.Ordinal)
            || string.Equals(value, Done, StringComparison.Ordinal))
        {
            return value;
        }

        return New;
    }

    public static bool TryGetNext(string current, out string next)
    {
        next = current switch
        {
            New => InProgress,
            InProgress => Done,
            _ => string.Empty,
        };
        return next.Length > 0;
    }

    // Club Manager: Mới → Đang xử lý | Đã xử lý; Đang xử lý → Đã xử lý (không quay lại)
    public static bool CanManagerTransition(string current, string wanted)
    {
        var from = NormalizeOrDefault(current);
        var to = (wanted ?? string.Empty).Trim();
        if (from == Done) return false;
        if (from == New) return to is InProgress or Done;
        if (from == InProgress) return to == Done;
        return false;
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
