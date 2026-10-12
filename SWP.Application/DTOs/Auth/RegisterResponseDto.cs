namespace SWP.Application.DTOs.Auth;

public class RegisterResponseDto
{
    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string RoleId { get; set; } = null!;

    public string Status { get; set; } = null!;
}
