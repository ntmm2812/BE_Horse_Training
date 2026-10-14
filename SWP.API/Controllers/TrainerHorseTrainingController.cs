using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP.Application.Common;
using SWP.Application.Services.Interface;
using SWP.Domain.Common;

namespace SWP.API.Controllers;

/// <summary> HLV Trưởng: xem thể trạng, gợi ý giáo án và lịch tuần của từng ngựa. </summary>
[ApiController]
[Route("api/trainer/horses")]
[Authorize(Roles = RoleNames.HeadTrainer)]
public class TrainerHorseTrainingController : TrainingControllerBase
{
    private readonly IHorseFitnessService _fitnessService;
    private readonly ITrainingPlanService _trainingPlanService;
    private readonly ITrainingSessionService _trainingSessionService;

    public TrainerHorseTrainingController(
        IHorseFitnessService fitnessService,
        ITrainingPlanService trainingPlanService,
        ITrainingSessionService trainingSessionService)
    {
        _fitnessService = fitnessService;
        _trainingPlanService = trainingPlanService;
        _trainingSessionService = trainingSessionService;
    }

    [HttpGet("{horseId:int}/fitness-assessment")]
    public async Task<IActionResult> GetFitnessAssessment(int horseId, CancellationToken cancellationToken)
    {
        var assessment = await _fitnessService.GetAssessmentAsync(horseId, cancellationToken);
        if (assessment is null)
        {
            return NotFound(new { message = "Không tìm thấy ngựa.", code = ServiceErrorCodes.NotFound });
        }

        return Ok(assessment);
    }

    /// <summary> Gợi ý giáo án theo thể trạng; startDate mặc định Thứ Hai tới, weeks mặc định 8. </summary>
    [HttpGet("{horseId:int}/plan-suggestion")]
    public async Task<IActionResult> GetPlanSuggestion(
        int horseId,
        [FromQuery] DateOnly? startDate,
        [FromQuery] int? weeks,
        CancellationToken cancellationToken)
    {
        var result = await _trainingPlanService.GetSuggestionAsync(horseId, startDate, weeks, cancellationToken);
        return FromResult(result);
    }

    /// <summary> Lịch 7 ngày (Thứ Hai → Chủ Nhật) của ngựa; weekStart là ngày bất kỳ trong tuần. </summary>
    [HttpGet("{horseId:int}/weekly-schedule")]
    public async Task<IActionResult> GetWeeklySchedule(
        int horseId,
        [FromQuery] DateOnly? weekStart,
        CancellationToken cancellationToken)
    {
        var result = await _trainingSessionService.GetHorseWeeklyScheduleAsync(horseId, weekStart, cancellationToken);
        return FromResult(result);
    }
}
