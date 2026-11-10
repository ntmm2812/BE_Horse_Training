using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP.Application.DTOs.Auth;
using SWP.Application.Services.Interface;

namespace SWP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);

        if (!result.Success)
        {
            return Unauthorized(new
            {
                message = result.ErrorMessage,
                code = result.ErrorCode,
            });
        }

        return Ok(result.Data);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(request, cancellationToken);

        if (!result.Success)
        {
            if (result.ErrorCode == "EMAIL_EXISTS")
            {
                return Conflict(new
                {
                    message = result.ErrorMessage,
                    code = result.ErrorCode,
                });
            }

            return BadRequest(new
            {
                message = result.ErrorMessage,
                code = result.ErrorCode,
            });
        }

        return StatusCode(StatusCodes.Status201Created, result.Data);
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequestDto request,
        CancellationToken cancellationToken)
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(sub) || !int.TryParse(sub, out var userId))
        {
            return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });
        }

        var result = await _authService.ChangePasswordAsync(userId, request, cancellationToken);
        if (!result.Success)
        {
            if (result.ErrorCode == "NOT_FOUND")
            {
                return NotFound(new { message = result.ErrorMessage, code = result.ErrorCode });
            }

            return BadRequest(new { message = result.ErrorMessage, code = result.ErrorCode });
        }

        return Ok(result.Data);
    }
}
