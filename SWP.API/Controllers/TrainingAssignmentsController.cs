using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP.Application.Services.Interface;
using SWP.Domain.Common;

namespace SWP.API.Controllers;

/// <summary> Nhân sự được HLV phân công xem lịch tập của mình. </summary>
[ApiController]
[Route("api/training-assignments")]
[Authorize(Roles = RoleNames.Groom + "," + RoleNames.HeadTrainer)]
public class TrainingAssignmentsController : TrainingControllerBase
{
    private readonly ITrainingSessionService _trainingSessionService;

    public TrainingAssignmentsController(ITrainingSessionService trainingSessionService)
    {
        _trainingSessionService = trainingSessionService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMine(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return UnknownUser();
        }

        var result = await _trainingSessionService.GetMyAssignmentsAsync(userId, from, to, cancellationToken);
        return FromResult(result);
    }
}
