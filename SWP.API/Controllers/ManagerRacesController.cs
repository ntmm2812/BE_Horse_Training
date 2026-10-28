using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP.Application.DTOs.Races;
using SWP.Application.Services.Interface;
using SWP.Domain.Common;

namespace SWP.API.Controllers;

[ApiController]
[Route("api/manager/races")]
[Authorize(Roles = RoleNames.ClubManager)]
public class ManagerRacesController : TrainingControllerBase
{
    private readonly IRaceService _raceService;

    public ManagerRacesController(IRaceService raceService)
    {
        _raceService = raceService;
    }

    /// <summary> Lấy danh sách toàn bộ giải đua (có thể lọc theo trạng thái). </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status, CancellationToken cancellationToken)
    {
        var result = await _raceService.GetAllRacesAsync(status, cancellationToken);
        return FromResult(result);
    }

    /// <summary> Xem chi tiết giải đua và danh sách các ngựa đăng ký. </summary>
    [HttpGet("{raceId:int}")]
    public async Task<IActionResult> GetById(int raceId, CancellationToken cancellationToken)
    {
        var result = await _raceService.GetRaceByIdAsync(raceId, cancellationToken);
        return FromResult(result);
    }

    /// <summary> Tạo giải đua mới. </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRaceDto dto, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return UnknownUser();
        }

        var result = await _raceService.CreateRaceAsync(dto, userId, cancellationToken);
        return FromResult(result, StatusCodes.Status201Created);
    }

    /// <summary> Cập nhật thông tin/trạng thái giải đua. </summary>
    [HttpPut("{raceId:int}")]
    public async Task<IActionResult> Update(int raceId, [FromBody] UpdateRaceDto dto, CancellationToken cancellationToken)
    {
        var result = await _raceService.UpdateRaceAsync(raceId, dto, cancellationToken);
        return FromResult(result);
    }

    /// <summary> Xóa giải đua (chỉ khi chưa có ngựa hoặc đã hủy). </summary>
    [HttpDelete("{raceId:int}")]
    public async Task<IActionResult> Delete(int raceId, CancellationToken cancellationToken)
    {
        var result = await _raceService.DeleteRaceAsync(raceId, cancellationToken);
        return NoContentFrom(result);
    }

    /// <summary> Duyệt hoặc cập nhật trạng thái đơn đăng ký thi đấu / gán số áo ngựa (HorseNumber). </summary>
    [HttpPatch("registrations/{registrationId:int}/status")]
    public async Task<IActionResult> UpdateRegistrationStatus(
        int registrationId,
        [FromBody] UpdateRaceRegistrationStatusDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _raceService.UpdateRegistrationStatusAsync(registrationId, dto, cancellationToken);
        return FromResult(result);
    }

    /// <summary> Cập nhật kết quả thi đấu (hạng về đích, thời gian, tiền thưởng) và hoàn tất giải đua. </summary>
    [HttpPost("{raceId:int}/results")]
    public async Task<IActionResult> RecordResults(
        int raceId,
        [FromBody] BatchRecordRaceResultsDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _raceService.RecordRaceResultsAsync(raceId, dto, cancellationToken);
        return FromResult(result);
    }

    /// <summary> Báo cáo tổng quan về các giải đấu và doanh thu thưởng của câu lạc bộ. </summary>
    [HttpGet("summary-report")]
    public async Task<IActionResult> GetSummaryReport(CancellationToken cancellationToken)
    {
        var result = await _raceService.GetClubRaceSummaryReportAsync(cancellationToken);
        return FromResult(result);
    }
}
