using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP.Application.Services.Interface;
using SWP.Domain.Common;

namespace SWP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HorsesController : ControllerBase
{
    private readonly IHorseService _horseService;

    public HorsesController(IHorseService horseService)
    {
        _horseService = horseService;
    }

    [HttpGet("health-statuses")]
    public async Task<IActionResult> GetHealthStatuses(CancellationToken cancellationToken)
    {
        if (IsHorseOwnerOnly())
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });
            }

            var ownerStatuses = await _horseService.GetHealthStatusesForOwnerAsync(userId, cancellationToken);
            return Ok(ownerStatuses);
        }

        var statuses = await _horseService.GetHealthStatusesAsync(cancellationToken);
        return Ok(statuses);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var horses = await _horseService.GetAllAsync(cancellationToken);
        return Ok(horses);
    }

    [HttpGet("{horseId:int}")]
    public async Task<IActionResult> GetById(int horseId, CancellationToken cancellationToken)
    {
        var horse = await _horseService.GetByIdAsync(horseId, cancellationToken);
        if (horse is null)
        {
            return NotFound(new { message = "Không tìm thấy ngựa." });
        }

        return Ok(horse);
    }

    private bool IsHorseOwnerOnly() =>
        User.IsInRole(RoleNames.HorseOwner) && !User.IsInRole(RoleNames.ClubManager);

    private bool TryGetCurrentUserId(out int userId)
    {
        userId = 0;
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return !string.IsNullOrWhiteSpace(sub) && int.TryParse(sub, out userId);
    }
}
