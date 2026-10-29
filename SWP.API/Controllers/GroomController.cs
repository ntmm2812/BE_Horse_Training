using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP.Application.Common;
using SWP.Application.DTOs.Care;
using SWP.Application.Services.Interface;
using SWP.Domain.Common;

namespace SWP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = RoleNames.Groom)]
public class GroomController : ControllerBase
{
    private readonly ICareService _careService;

    public GroomController(ICareService careService)
    {
        _careService = careService;
    }

    [HttpGet("tasks")]
    public async Task<IActionResult> GetTasks(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return UnknownUser();
        }

        return Ok(await _careService.GetTasksAsync(userId, cancellationToken));
    }

    [HttpPost("tasks/{taskId:int}/complete")]
    public async Task<IActionResult> CompleteTask(int taskId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return UnknownUser();
        }

        var result = await _careService.CompleteTaskAsync(taskId, userId, cancellationToken);
        if (!result.Success)
        {
            return Error(result);
        }

        return Ok(result.Data);
    }

    [HttpGet("stables")]
    public async Task<IActionResult> GetStables(CancellationToken cancellationToken)
    {
        return Ok(await _careService.GetStablesAsync(cancellationToken));
    }

    [HttpGet("feeding")]
    public async Task<IActionResult> GetFeeding(CancellationToken cancellationToken)
    {
        return Ok(await _careService.GetFeedingAsync(cancellationToken));
    }

    [HttpGet("incidents")]
    public async Task<IActionResult> GetIncidents(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return UnknownUser();
        }

        return Ok(await _careService.GetIncidentsAsync(userId, cancellationToken));
    }

    [HttpPost("incidents")]
    public async Task<IActionResult> CreateIncident(
        [FromBody] CreateIncidentReportRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return UnknownUser();
        }

        var result = await _careService.CreateIncidentAsync(request, userId, cancellationToken);
        if (!result.Success)
        {
            return Error(result);
        }

        return Ok(result.Data);
    }

    private IActionResult Error<T>(ServiceResult<T> result)
    {
        var body = new { message = result.ErrorMessage, code = result.ErrorCode };
        if (result.ErrorCode == ServiceErrorCodes.NotFound)
        {
            return NotFound(body);
        }

        if (result.ErrorCode == ServiceErrorCodes.Conflict || result.ErrorCode == ServiceErrorCodes.InvalidStatus)
        {
            return Conflict(body);
        }

        return BadRequest(body);
    }

    private IActionResult UnknownUser() =>
        Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

    private bool TryGetCurrentUserId(out int userId)
    {
        userId = 0;
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrWhiteSpace(sub) && int.TryParse(sub, out userId);
    }
}
