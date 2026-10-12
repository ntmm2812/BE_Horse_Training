using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP.Application.Services.Interface;
using SWP.Domain.Common;

namespace SWP.API.Controllers;

[ApiController]
[Route("api/owner/horses")]
[Authorize(Roles = RoleNames.HorseOwner)]
public class OwnerHorsesController : ControllerBase
{
    private readonly IHorseService _horseService;

    public OwnerHorsesController(IHorseService horseService)
    {
        _horseService = horseService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyHorses(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });
        }

        var horses = await _horseService.GetByOwnerAsync(userId, cancellationToken);
        return Ok(horses);
    }

    [HttpGet("{horseId:int}")]
    public async Task<IActionResult> GetMyHorseById(int horseId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });
        }

        var detail = await _horseService.GetDetailForOwnerAsync(horseId, userId, cancellationToken);
        if (detail is null)
        {
            return NotFound(new { message = "Không tìm thấy ngựa hoặc ngựa không thuộc quyền sở hữu của bạn." });
        }

        return Ok(detail);
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        userId = 0;
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return !string.IsNullOrWhiteSpace(sub) && int.TryParse(sub, out userId);
    }
}
