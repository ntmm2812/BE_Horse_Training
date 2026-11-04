using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP.Application.Common;
using SWP.Application.DTOs.Care;
using SWP.Application.Services.Interface;
using SWP.Domain.Common;

namespace SWP.API.Controllers;

// Club Manager: xem mọi sự cố; chỉ được cập nhật trạng thái sự cố chuồng trại / thiết bị.
[ApiController]
[Route("api/manager/incidents")]
[Authorize(Roles = RoleNames.ClubManager)]
public class ManagerIncidentsController : ControllerBase
{
    private readonly ICareService _careService;

    public ManagerIncidentsController(ICareService careService)
    {
        _careService = careService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var reports = await _careService.GetAllIncidentsForManagerAsync(cancellationToken);
        return Ok(reports);
    }

    [HttpPost("{reportId:int}/status")]
    public async Task<IActionResult> UpdateFacilityStatus(
        int reportId,
        [FromBody] UpdateIncidentStatusRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var managerId))
        {
            return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });
        }

        var result = await _careService.UpdateFacilityIncidentStatusAsync(reportId, request, managerId, cancellationToken);
        if (!result.Success)
        {
            return Error(result);
        }

        return Ok(result.Data);
    }

    private static IActionResult Error<T>(ServiceResult<T> result)
    {
        var body = new { message = result.ErrorMessage, code = result.ErrorCode };
        if (result.ErrorCode == ServiceErrorCodes.NotFound)
        {
            return new NotFoundObjectResult(body);
        }

        if (result.ErrorCode == ServiceErrorCodes.Conflict || result.ErrorCode == ServiceErrorCodes.InvalidStatus)
        {
            return new ConflictObjectResult(body);
        }

        return new BadRequestObjectResult(body);
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        userId = 0;
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrWhiteSpace(sub) && int.TryParse(sub, out userId);
    }
}
