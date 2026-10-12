using SWP.Application.DTOs.Users;

namespace SWP.Application.Common;

public class ApprovalResult
{
    public bool Success { get; init; }

    public PendingUserDto? Data { get; init; }

    public string? ErrorMessage { get; init; }

    public string? ErrorCode { get; init; }

    public static ApprovalResult Ok(PendingUserDto data) =>
        new() { Success = true, Data = data };

    public static ApprovalResult Fail(string message, string? errorCode = null) =>
        new() { Success = false, ErrorMessage = message, ErrorCode = errorCode };
}
