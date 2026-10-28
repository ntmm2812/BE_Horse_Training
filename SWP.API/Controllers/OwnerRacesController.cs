using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP.Application.Services.Interface;
using SWP.Domain.Common;

namespace SWP.API.Controllers;

[ApiController]
[Route("api/owner/races")]
[Authorize(Roles = RoleNames.HorseOwner)]
public class OwnerRacesController : TrainingControllerBase
{
    private readonly IRaceService _raceService;

    public OwnerRacesController(IRaceService raceService)
    {
        _raceService = raceService;
    }

    /// <summary> Xem danh sách toàn bộ các giải đấu mà đàn ngựa của chủ sở hữu đã/đang đăng ký tham gia. </summary>
    [HttpGet]
    public async Task<IActionResult> GetMyRaces(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return UnknownUser();
        }

        var result = await _raceService.GetOwnerRacesAsync(userId, cancellationToken);
        return FromResult(result);
    }

    /// <summary> Xem báo cáo thành tích, lịch sử thi đấu và danh hiệu của một con ngựa cụ thể thuộc sở hữu. </summary>
    [HttpGet("horses/{horseId:int}/performance")]
    public async Task<IActionResult> GetHorsePerformance(int horseId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return UnknownUser();
        }

        var result = await _raceService.GetHorsePerformanceReportAsync(horseId, userId, cancellationToken);
        return FromResult(result);
    }

    /// <summary> Báo cáo tổng hợp thành tích thi đấu, tỷ lệ chiến thắng và doanh thu tiền thưởng định kỳ của toàn bộ đàn ngựa. </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return UnknownUser();
        }

        var result = await _raceService.GetOwnerPerformanceSummaryAsync(userId, cancellationToken);
        return FromResult(result);
    }
}
