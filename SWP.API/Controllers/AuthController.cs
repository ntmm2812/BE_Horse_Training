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
}
