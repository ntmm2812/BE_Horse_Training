using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    public TrainerTrainingSessionsController(ITrainingSessionService trainingSessionService)
    {
        _trainingSessionService = trainingSessionService;
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
        return FromResult(result);
    }

    [HttpPut("{sessionId:int}/assignment")]
    public async Task<IActionResult> Assign(
        int sessionId,
        [FromBody] AssignTrainingSessionRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _trainingSessionService.AssignAsync(sessionId, request, cancellationToken);
        return FromResult(result);
    }

    [HttpPost("{sessionId:int}/start")]
    public async Task<IActionResult> Start(int sessionId, CancellationToken cancellationToken)
    {
        var result = await _trainingSessionService.StartAsync(sessionId, cancellationToken);
        return FromResult(result);
    }

    /// <summary> Ghi nhận chỉ số, điểm phong độ và nhận xét chuyên môn sau buổi tập. </summary>
    [HttpPost("{sessionId:int}/complete")]
    public async Task<IActionResult> Complete(
        int sessionId,
        [FromBody] CompleteTrainingSessionRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _trainingSessionService.CompleteAsync(sessionId, request, cancellationToken);
        return FromResult(result);
    }

    [HttpPost("{sessionId:int}/cancel")]
    public async Task<IActionResult> Cancel(
        int sessionId,
        [FromBody] CancelTrainingSessionRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _trainingSessionService.CancelAsync(sessionId, request, cancellationToken);
        return FromResult(result);
    }

    [HttpDelete("{sessionId:int}")]
    public async Task<IActionResult> Delete(int sessionId, CancellationToken cancellationToken)
    {
        var result = await _trainingSessionService.DeleteAsync(sessionId, cancellationToken);
        return NoContentFrom(result);
    }
}
