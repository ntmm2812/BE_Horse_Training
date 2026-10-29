namespace SWP.Domain.Common;

public static class VaccineStatuses
{
    public const string Pending = "Chờ tiêm";

    public const string Done = "Đã tiêm";
}

public static class InjuryCatalog
{
    public const string None = "Không có chấn thương";

    public const string Separator = ", ";

    public static readonly string[] Locations =
    [
        "Đầu/cổ",
        "Vai",
        "Lưng",
        "Hông",
        "Bụng",
        "Chân trước trái",
        "Chân trước phải",
        "Chân sau trái",
        "Chân sau phải",
        "Gân",
        "Khớp",
        "Móng",
    ];

    public static bool TryNormalize(string? raw, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var parts = raw.Split(Separator, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        if (parts.Contains(None, StringComparer.Ordinal))
        {
            if (parts.Length != 1)
            {
                return false;
            }

            normalized = None;
            return true;
        }

        if (parts.Any(part => !Locations.Contains(part, StringComparer.Ordinal)))
        {
            return false;
        }

        normalized = string.Join(Separator, parts.Distinct(StringComparer.Ordinal));
        return true;
    }

    public static bool HasInjury(string normalized) =>
        !string.Equals(normalized, None, StringComparison.Ordinal);
}

public static class VaccineCatalog
{
    public static readonly string[] SuggestedTypes =
    [
        "Cúm ngựa",
        "Uốn ván",
        "Viêm não Nhật Bản",
        "Dại",
        "Tẩy giun",
    ];

    public const int MinLength = 2;

    public const int MaxLength = 50;

    public static string? Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var collapsed = string.Join(' ', raw.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        var known = SuggestedTypes.FirstOrDefault(type =>
            string.Equals(type, collapsed, StringComparison.CurrentCultureIgnoreCase));
        if (known is not null)
        {
            return known;
        }

        if (collapsed.Length < MinLength || collapsed.Length > MaxLength)
        {
            return null;
        }

        return char.ToUpper(collapsed[0], System.Globalization.CultureInfo.GetCultureInfo("vi-VN")) + collapsed[1..];
    }
}
