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
    private readonly IMedicalService _medicalService;

    public HorsesController(IHorseService horseService, IMedicalService medicalService)
    {
        _horseService = horseService;
        _medicalService = medicalService;
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

    [HttpGet("training-locks")]
    public async Task<IActionResult> GetTrainingLocks(CancellationToken cancellationToken)
    {
        var locks = await _medicalService.GetTrainingLocksAsync(cancellationToken);
        return Ok(locks);
    }

    [HttpGet("training-locks/active")]
    public async Task<IActionResult> GetActiveTrainingLocks(CancellationToken cancellationToken)
    {
        var locks = await _medicalService.GetActiveTrainingLocksAsync(cancellationToken);
        return Ok(locks);
    }

    [HttpGet("vaccinations")]
    public async Task<IActionResult> GetVaccinations([FromQuery] int? horseId, CancellationToken cancellationToken)
    {
        var list = await _medicalService.GetVaccinationsAsync(horseId, cancellationToken);
        return Ok(list);
    }

    [HttpGet("{horseId:int}/latest-exam")]
    public async Task<IActionResult> GetLatestExamDate(int horseId, CancellationToken cancellationToken)
    {
        var result = await _medicalService.GetLatestExamDateAsync(horseId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("vets")]
    public async Task<IActionResult> GetVets(CancellationToken cancellationToken)
    {
        var vets = await _medicalService.GetVetsAsync(cancellationToken);
        return Ok(vets);
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
