using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP.Application.Services.Interface;
using SWP.Domain.Common;

namespace SWP.API.Controllers;

/// <summary> Chủ ngựa: đánh giá buổi tập ĐÃ ĐƯỢC HLV GỬI, chỉ của ngựa thuộc chủ đó. </summary>
[ApiController]
[Route("api/owner/session-reviews")]
[Authorize(Roles = RoleNames.HorseOwner)]
public class OwnerSessionReviewsController : TrainingControllerBase
{
    private readonly ITrainingSessionService _trainingSessionService;

    public OwnerSessionReviewsController(ITrainingSessionService trainingSessionService)
    {
        _trainingSessionService = trainingSessionService;
    }

    /// <summary> Mới nhất trước; lọc theo ngựa, khoảng ngày buổi tập và số lượng tối đa. </summary>
    [HttpGet]
    public async Task<IActionResult> GetMine(
        [FromQuery] int? horseId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return UnknownUser();
        }

        var result = await _trainingSessionService.GetOwnerReviewsAsync(userId, horseId, from, to, limit, cancellationToken);
        return FromResult(result);
    }
}
