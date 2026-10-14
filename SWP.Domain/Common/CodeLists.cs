namespace SWP.Domain.Common;

/// <summary> Tra cứu mã (code) không phân biệt hoa thường, trả về đúng cách viết chuẩn. </summary>
internal static class CodeLists
{
    public static bool TryNormalize(IReadOnlyList<string> all, string? value, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        foreach (var code in all)
        {
            if (string.Equals(code, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                normalized = code;
                return true;
            }
        }

        return false;
    }
}
