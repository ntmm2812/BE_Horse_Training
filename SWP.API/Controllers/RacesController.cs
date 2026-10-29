using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP.Application.Common;
using SWP.Application.DTOs.Race;
using SWP.Application.Services.Interface;
using SWP.Domain.Common;

namespace SWP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RacesController : ControllerBase
{
    private const string ReadRoles =
        RoleNames.HeadTrainer + "," + RoleNames.Veterinarian + "," + RoleNames.ClubManager + "," + RoleNames.HorseOwner;

    private readonly IRaceService _races;

    public RacesController(IRaceService races)
    {
        _races = races;
    }

    [HttpGet]
    [Authorize(Roles = ReadRoles)]
    public async Task<IActionResult> GetRaces(CancellationToken cancellationToken)
    {
        return Ok(await _races.GetRacesAsync(cancellationToken));
    }

    [HttpGet("registrations")]
    [Authorize(Roles = ReadRoles)]
    public async Task<IActionResult> GetRegistrations(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return UnknownUser();
        }

        int? ownerId = User.IsInRole(RoleNames.HorseOwner) ? userId : null;
        return Ok(await _races.GetRegistrationsAsync(ownerId, cancellationToken));
    }

    [HttpPost("{raceId:int}/registrations")]
    [Authorize(Roles = RoleNames.HeadTrainer)]
    public async Task<IActionResult> Register(
        int raceId,
        [FromBody] RegisterRaceRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return UnknownUser();
        }

        var result = await _races.RegisterAsync(raceId, request.HorseID, userId, cancellationToken);
        return result.Success ? Ok(result.Data) : Error(result);
    }

    [HttpDelete("{raceId:int}/registrations/{horseId:int}")]
    [Authorize(Roles = RoleNames.HeadTrainer)]
    public async Task<IActionResult> Cancel(int raceId, int horseId, CancellationToken cancellationToken)
    {
        var result = await _races.CancelAsync(raceId, horseId, cancellationToken);
        return result.Success ? NoContent() : Error(result);
    }

    [HttpPut("{raceId:int}/registrations/{horseId:int}/result")]
    [Authorize(Roles = RoleNames.HeadTrainer)]
    public async Task<IActionResult> UpdateResult(
        int raceId,
        int horseId,
        [FromBody] UpdateRaceResultRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _races.UpdateResultAsync(raceId, horseId, request.ThuHang, cancellationToken);
        return result.Success ? Ok(result.Data) : Error(result);
    }

    [HttpPut("{raceId:int}/registrations/{horseId:int}/prize")]
    [Authorize(Roles = RoleNames.ClubManager)]
    public async Task<IActionResult> SetPrize(
        int raceId,
        int horseId,
        [FromBody] SetRacePrizeRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _races.SetPrizeAsync(raceId, horseId, request.Amount, cancellationToken);
        return result.Success ? Ok(result.Data) : Error(result);
    }

    [HttpGet("prize-alerts")]
    [Authorize(Roles = RoleNames.ClubManager)]
    public async Task<IActionResult> GetPrizeAlerts(CancellationToken cancellationToken)
    {
        return Ok(await _races.GetPrizeAlertsAsync(cancellationToken));
    }

    [HttpDelete("prize-alerts/{alertId:int}")]
    [Authorize(Roles = RoleNames.ClubManager)]
    public async Task<IActionResult> DismissPrizeAlert(int alertId, CancellationToken cancellationToken)
    {
        var result = await _races.DismissPrizeAlertAsync(alertId, cancellationToken);
        return result.Success ? NoContent() : Error(result);
    }

    private IActionResult Error<T>(ServiceResult<T> result)
    {
        var body = new { message = result.ErrorMessage, code = result.ErrorCode };
        if (result.ErrorCode == ServiceErrorCodes.NotFound)
        {
            return NotFound(body);
        }

        if (result.ErrorCode == ServiceErrorCodes.Conflict || result.ErrorCode == ServiceErrorCodes.InvalidStatus)
        {
            return Conflict(body);
        }

        return BadRequest(body);
    }

    private IActionResult UnknownUser() =>
        Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

    private bool TryGetCurrentUserId(out int userId)
    {
        userId = 0;
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrWhiteSpace(sub) && int.TryParse(sub, out userId);
    }
}
