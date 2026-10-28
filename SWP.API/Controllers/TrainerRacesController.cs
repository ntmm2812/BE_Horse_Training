using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP.Application.DTOs.Races;
using SWP.Application.Services.Interface;
using SWP.Domain.Common;

namespace SWP.API.Controllers;

[ApiController]
[Route("api/trainer/races")]
[Authorize(Roles = RoleNames.HeadTrainer)]
public class TrainerRacesController : TrainingControllerBase
{
    private readonly IRaceService _raceService;

    public TrainerRacesController(IRaceService raceService)
    {
        _raceService = raceService;
    }

    /// <summary> Xem danh sách các giải đua đang mở tiếp nhận đăng ký. </summary>
    [HttpGet("open")]
    public async Task<IActionResult> GetOpenRaces(CancellationToken cancellationToken)
    {
        var result = await _raceService.GetOpenRacesAsync(cancellationToken);
        return FromResult(result);
    }

    /// <summary> Lựa chọn chiến mã: Kiểm tra danh sách chiến mã và điều kiện sẵn sàng tham gia giải đua. </summary>
    [HttpGet("{raceId:int}/eligible-horses")]
    public async Task<IActionResult> GetEligibleHorses(int raceId, CancellationToken cancellationToken)
    {
        var result = await _raceService.GetEligibleHorsesForRaceAsync(raceId, cancellationToken);
        return FromResult(result);
    }

    /// <summary> Đăng ký chiến mã tham gia giải đua phù hợp. </summary>
    [HttpPost("{raceId:int}/register")]
    public async Task<IActionResult> RegisterHorse(
        int raceId,
        [FromBody] RegisterHorseForRaceDto dto,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return UnknownUser();
        }

        var result = await _raceService.RegisterHorseForRaceAsync(raceId, dto, userId, cancellationToken);
        return FromResult(result, StatusCodes.Status201Created);
    }

    /// <summary> Hủy đơn đăng ký giải đua (trước khi giải bắt đầu). </summary>
    [HttpDelete("registrations/{registrationId:int}")]
    public async Task<IActionResult> CancelRegistration(int registrationId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return UnknownUser();
        }

        var result = await _raceService.CancelRegistrationAsync(registrationId, userId, isManager: false, cancellationToken);
        return NoContentFrom(result);
    }
}
