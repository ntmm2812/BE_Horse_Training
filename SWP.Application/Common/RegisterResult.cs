using SWP.Application.DTOs.Auth;

namespace SWP.Application.Common;

public class RegisterResult
{
    public bool Success { get; init; }

    public RegisterResponseDto? Data { get; init; }

    public string? ErrorMessage { get; init; }

    public string? ErrorCode { get; init; }

    public static RegisterResult Ok(RegisterResponseDto data) =>
        new() { Success = true, Data = data };

    public static RegisterResult Fail(string message, string? errorCode = null) =>
        new() { Success = false, ErrorMessage = message, ErrorCode = errorCode };
}
