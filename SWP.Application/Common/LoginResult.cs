using SWP.Application.DTOs.Auth;

namespace SWP.Application.Common;

public class LoginResult
{
    public bool Success { get; init; }

    public LoginResponseDto? Data { get; init; }

    public string? ErrorMessage { get; init; }

    public string? ErrorCode { get; init; }

    public static LoginResult Ok(LoginResponseDto data) =>
        new() { Success = true, Data = data };

    public static LoginResult Fail(string message, string? errorCode = null) =>
        new() { Success = false, ErrorMessage = message, ErrorCode = errorCode };
}
