namespace SWP.Domain.Common;

public static class RoleMapper
{
    private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["head trainer"] = "head_trainer",
        ["headtrainer"] = "head_trainer",
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

    public static string ToRoleId(string? roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return string.Empty;
        }

        var trimmed = roleName.Trim();
        if (Aliases.TryGetValue(trimmed, out var mapped))
        {
            return mapped;
        }

        var snake = trimmed.ToLowerInvariant().Replace(' ', '_').Replace('-', '_');
        if (ValidRoleIds.Contains(snake))
        {
            return snake;
        }

        return trimmed;
    }
}
