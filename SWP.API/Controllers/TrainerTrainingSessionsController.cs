using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP.Application.Common;
using SWP.Application.DTOs.Training;
using SWP.Application.Services.Interface;
using SWP.Domain.Common;

namespace SWP.API.Controllers;

/// <summary> HLV Trưởng: điều chỉnh, phân công và thực hiện từng buổi tập. </summary>
[ApiController]
[Route("api/trainer/training-sessions")]
[Authorize(Roles = RoleNames.HeadTrainer)]
public class TrainerTrainingSessionsController : TrainingControllerBase
{
    private readonly ITrainingSessionService _trainingSessionService;
    private readonly IHorseRealtimeNotifier _realtime;

    public TrainerTrainingSessionsController(
        ITrainingSessionService trainingSessionService,
        IHorseRealtimeNotifier realtime)
    {
        _trainingSessionService = trainingSessionService;
        _realtime = realtime;
    }

    /// <summary> Lịch tập theo khoảng ngày (mặc định 7 ngày từ hôm nay), lọc theo ngựa/giáo án/người phụ trách/trạng thái. </summary>
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] TrainingSessionQueryDto query, CancellationToken cancellationToken)
    {
        var result = await _trainingSessionService.SearchAsync(query, cancellationToken);
        return FromResult(result);
    }

    /// <summary> Nhân sự có thể phân công (Nhân viên chăm sóc, HLV). </summary>
    [HttpGet("assignable-staff")]
    public async Task<IActionResult> GetAssignableStaff(CancellationToken cancellationToken)
    {
        var staff = await _trainingSessionService.GetAssignableStaffAsync(cancellationToken);
        return Ok(staff);
    }

    [HttpGet("{sessionId:int}")]
    public async Task<IActionResult> GetById(int sessionId, CancellationToken cancellationToken)
    {
        var result = await _trainingSessionService.GetByIdAsync(sessionId, cancellationToken);
        return FromResult(result);
    }

    [HttpPut("{sessionId:int}")]
    public async Task<IActionResult> Update(
        int sessionId,
        [FromBody] SaveTrainingSessionRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _trainingSessionService.UpdateAsync(sessionId, request, cancellationToken);
        return await FromResultNotify(result, HorseRealtimeKinds.TapLuyen, cancellationToken);
    }

    [HttpPut("{sessionId:int}/assignment")]
    public async Task<IActionResult> Assign(
        int sessionId,
        [FromBody] AssignTrainingSessionRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _trainingSessionService.AssignAsync(sessionId, request, cancellationToken);
        return await FromResultNotify(result, HorseRealtimeKinds.TapLuyen, cancellationToken);
    }

    [HttpPost("{sessionId:int}/start")]
    public async Task<IActionResult> Start(int sessionId, CancellationToken cancellationToken)
    {
        var result = await _trainingSessionService.StartAsync(sessionId, cancellationToken);
        return await FromResultNotify(result, HorseRealtimeKinds.TapLuyen, cancellationToken);
    }

    /// <summary> Ghi nhận chỉ số, điểm phong độ và nhận xét chuyên môn sau buổi tập. </summary>
    [HttpPost("{sessionId:int}/complete")]
    public async Task<IActionResult> Complete(
        int sessionId,
        [FromBody] CompleteTrainingSessionRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _trainingSessionService.CompleteAsync(sessionId, request, cancellationToken);
        return await FromResultNotify(result, HorseRealtimeKinds.TapLuyen, cancellationToken);
    }

    [HttpPost("{sessionId:int}/cancel")]
    public async Task<IActionResult> Cancel(
        int sessionId,
        [FromBody] CancelTrainingSessionRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _trainingSessionService.CancelAsync(sessionId, request, cancellationToken);
        return await FromResultNotify(result, HorseRealtimeKinds.TapLuyen, cancellationToken);
    }

    [HttpDelete("{sessionId:int}")]
    public async Task<IActionResult> Delete(int sessionId, CancellationToken cancellationToken)
    {
        var existing = await _trainingSessionService.GetByIdAsync(sessionId, cancellationToken);
        var result = await _trainingSessionService.DeleteAsync(sessionId, cancellationToken);
        if (result.Success && existing.Success && existing.Data is not null)
        {
            await _realtime.NotifyHorseChangedAsync(existing.Data.HorseId, HorseRealtimeKinds.TapLuyen, cancellationToken);
        }

        return NoContentFrom(result);
    }

    private async Task<IActionResult> FromResultNotify(
        ServiceResult<TrainingSessionDto> result,
        string kind,
        CancellationToken cancellationToken)
    {
        if (result.Success && result.Data is not null)
        {
            await _realtime.NotifyHorseChangedAsync(result.Data.HorseId, kind, cancellationToken);
        }

        return FromResult(result);
    }
}
