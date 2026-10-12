namespace SWP.Application.DTOs.Auth;

public class LoginResponseDto
{
    public string Token { get; set; } = null!;

    public AuthUserDto User { get; set; } = null!;
}

public class AuthUserDto
{
    public int UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string RoleId { get; set; } = null!;

    public string Status { get; set; } = null!;
}
