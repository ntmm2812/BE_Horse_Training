namespace SWP.Domain.Common;

public static class RoleMapper
{
    private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["head trainer"] = "head_trainer",
        ["headtrainer"] = "head_trainer",
        ["veterinarian"] = "veterinarian",
        ["groom / stable hand"] = "groom",
        ["groom stable hand"] = "groom",
        ["groom"] = "groom",
        ["horse owner"] = "horse_owner",
        ["horseowner"] = "horse_owner",
        ["club manager"] = "club_manager",
        ["clubmanager"] = "club_manager",
        ["hlv trưởng"] = "head_trainer",
        ["bác sĩ thú y"] = "veterinarian",
        ["nhân viên chăm sóc"] = "groom",
        ["chủ ngựa"] = "horse_owner",
        ["quản lý clb"] = "club_manager",
    };

    private static readonly HashSet<string> ValidRoleIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "head_trainer",
        "veterinarian",
        "groom",
        "horse_owner",
        "club_manager",
    };

    public static bool IsCanonicalRoleId(string? roleId) =>
        !string.IsNullOrWhiteSpace(roleId) && ValidRoleIds.Contains(roleId.Trim());

    public static bool TryMapToCanonicalRoleId(string? roleName, out string canonicalRoleId)
    {
        canonicalRoleId = ToRoleId(roleName);
        return IsCanonicalRoleId(canonicalRoleId);
    }

    public static string ToRoleId(string? roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return string.Empty;
        }

        var trimmed = roleName.Trim();

        if (ValidRoleIds.Contains(trimmed))
        {
            return trimmed.ToLowerInvariant();
        }

        var lookupKey = NormalizeForLookup(trimmed);
        if (Aliases.TryGetValue(lookupKey, out var mapped))
        {
            return mapped;
        }

        var snake = lookupKey.Replace(' ', '_');
        if (ValidRoleIds.Contains(snake))
        {
            return snake;
        }

        var inferred = InferCanonicalFromTokens(lookupKey);
        if (inferred is not null)
        {
            return inferred;
        }

        return string.Empty;
    }

    private static string? InferCanonicalFromTokens(string lookupKey)
    {
        if (lookupKey.Contains("horse", StringComparison.Ordinal) && lookupKey.Contains("owner", StringComparison.Ordinal))
        {
            return "horse_owner";
        }

        if (lookupKey.Contains("club", StringComparison.Ordinal) && lookupKey.Contains("manager", StringComparison.Ordinal))
        {
            return "club_manager";
        }

        if (lookupKey.Contains("head", StringComparison.Ordinal) && lookupKey.Contains("trainer", StringComparison.Ordinal))
        {
            return "head_trainer";
        }

        if (lookupKey.Contains("veterinarian", StringComparison.Ordinal))
        {
            return "veterinarian";
        }

        if (lookupKey.Contains("groom", StringComparison.Ordinal)
            || lookupKey.Contains("stable hand", StringComparison.Ordinal))
        {
            return "groom";
        }

        return null;
    }

    private static string NormalizeForLookup(string value)
    {
        var normalized = value
            .Replace('\u00A0', ' ')
            .Replace('/', ' ')
            .Replace('-', ' ')
            .Replace('_', ' ')
            .Trim()
            .ToLowerInvariant();

        return string.Join(' ', normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
