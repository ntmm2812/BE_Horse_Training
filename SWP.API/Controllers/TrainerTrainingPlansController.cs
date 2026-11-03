using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP.Application.Common;
using SWP.Application.DTOs.Training;
using SWP.Application.Services.Interface;
using SWP.Domain.Common;

namespace SWP.API.Controllers;

/// <summary> HLV Trưởng: lập giáo án theo giai đoạn, áp dụng và xếp lịch tập hàng tuần. </summary>
[ApiController]
[Route("api/trainer/training-plans")]
[Authorize(Roles = RoleNames.HeadTrainer)]
public class TrainerTrainingPlansController : TrainingControllerBase
{
    private readonly ITrainingPlanService _trainingPlanService;
    private readonly ITrainingSessionService _trainingSessionService;
    private readonly IHorseRealtimeNotifier _realtime;

    public TrainerTrainingPlansController(
        ITrainingPlanService trainingPlanService,
        ITrainingSessionService trainingSessionService,
        IHorseRealtimeNotifier realtime)
    {
        _trainingPlanService = trainingPlanService;
        _trainingSessionService = trainingSessionService;
        _realtime = realtime;
    }

    /// <summary> Danh mục mã + nhãn tiếng Việt cho các dropdown của FE. </summary>
    [HttpGet("options")]
    public IActionResult GetOptions() => Ok(_trainingPlanService.GetOptions());

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] int? horseId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var result = await _trainingPlanService.SearchAsync(horseId, status, cancellationToken);
        return FromResult(result);
    }

    [HttpGet("{planId:int}")]
    public async Task<IActionResult> GetById(int planId, CancellationToken cancellationToken)
    {
        var result = await _trainingPlanService.GetDetailAsync(planId, cancellationToken);
        return FromResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateTrainingPlanRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return UnknownUser();
        }

        var result = await _trainingPlanService.CreateAsync(request, userId, cancellationToken);
        return await FromPlanResultNotify(result, StatusCodes.Status201Created, cancellationToken);
    }

    [HttpPut("{planId:int}")]
    public async Task<IActionResult> Update(
        int planId,
        [FromBody] UpdateTrainingPlanRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _trainingPlanService.UpdateAsync(planId, request, cancellationToken);
        return await FromPlanResultNotify(result, StatusCodes.Status200OK, cancellationToken);
    }

    [HttpDelete("{planId:int}")]
    public async Task<IActionResult> Delete(int planId, CancellationToken cancellationToken)
    {
        var existing = await _trainingPlanService.GetDetailAsync(planId, cancellationToken);
        var result = await _trainingPlanService.DeleteAsync(planId, cancellationToken);
        if (result.Success && existing.Success && existing.Data is not null)
        {
            await _realtime.NotifyHorseChangedAsync(existing.Data.HorseId, HorseRealtimeKinds.TapLuyen, cancellationToken);
        }

        return NoContentFrom(result);
    }

    [HttpPost("{planId:int}/activate")]
    public async Task<IActionResult> Activate(int planId, CancellationToken cancellationToken)
    {
        var result = await _trainingPlanService.ActivateAsync(planId, cancellationToken);
        return await FromPlanResultNotify(result, StatusCodes.Status200OK, cancellationToken);
    }

    [HttpPost("{planId:int}/complete")]
    public async Task<IActionResult> Complete(int planId, CancellationToken cancellationToken)
    {
        var result = await _trainingPlanService.CompleteAsync(planId, cancellationToken);
        return await FromPlanResultNotify(result, StatusCodes.Status200OK, cancellationToken);
    }

    [HttpPost("{planId:int}/cancel")]
    public async Task<IActionResult> Cancel(int planId, CancellationToken cancellationToken)
    {
        var result = await _trainingPlanService.CancelAsync(planId, cancellationToken);
        return await FromPlanResultNotify(result, StatusCodes.Status200OK, cancellationToken);
    }

    /// <summary> Sinh lịch tập 1 tuần từ các giai đoạn (có xem trước bằng previewOnly). </summary>
    [HttpPost("{planId:int}/weekly-schedule")]
    public async Task<IActionResult> GenerateWeeklySchedule(
        int planId,
        [FromBody] GenerateWeeklyScheduleRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _trainingPlanService.GenerateWeeklyScheduleAsync(planId, request, cancellationToken);
        if (result.Success && !request.PreviewOnly)
        {
            var plan = await _trainingPlanService.GetDetailAsync(planId, cancellationToken);
            if (plan.Success && plan.Data is not null)
            {
                await _realtime.NotifyHorseChangedAsync(plan.Data.HorseId, HorseRealtimeKinds.TapLuyen, cancellationToken);
            }
        }

        return FromResult(result, request.PreviewOnly ? StatusCodes.Status200OK : StatusCodes.Status201Created);
    }

    /// <summary> Thêm thủ công một buổi tập vào giáo án. </summary>
    [HttpPost("{planId:int}/sessions")]
    public async Task<IActionResult> CreateSession(
        int planId,
        [FromBody] SaveTrainingSessionRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _trainingSessionService.CreateAsync(planId, request, cancellationToken);
        if (result.Success && result.Data is not null)
        {
            await _realtime.NotifyHorseChangedAsync(result.Data.HorseId, HorseRealtimeKinds.TapLuyen, cancellationToken);
        }

        return FromResult(result, StatusCodes.Status201Created);
    }

    private async Task<IActionResult> FromPlanResultNotify<T>(
        ServiceResult<T> result,
        int successStatusCode,
        CancellationToken cancellationToken)
        where T : TrainingPlanDto
    {
        if (result.Success && result.Data is not null)
        {
            await _realtime.NotifyHorseChangedAsync(result.Data.HorseId, HorseRealtimeKinds.TapLuyen, cancellationToken);
        }

        return FromResult(result, successStatusCode);
    }
}
