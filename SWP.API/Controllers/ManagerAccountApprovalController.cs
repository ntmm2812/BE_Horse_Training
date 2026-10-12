using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP.Application.Services.Interface;
using SWP.Domain.Common;

namespace SWP.API.Controllers;

[ApiController]
[Route("api/manager/account-approvals")]
[Authorize(Roles = RoleNames.ClubManager)]
public class ManagerAccountApprovalController : ControllerBase
{
    private readonly IAccountApprovalService _accountApprovalService;

    public ManagerAccountApprovalController(IAccountApprovalService accountApprovalService)
    {
        _accountApprovalService = accountApprovalService;
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPending(CancellationToken cancellationToken)
    {
        var users = await _accountApprovalService.GetPendingUsersAsync(cancellationToken);
        return Ok(users);
    }

    [HttpPost("{userId:int}/approve")]
    public async Task<IActionResult> Approve(int userId, CancellationToken cancellationToken)
    {
        var result = await _accountApprovalService.ApproveUserAsync(userId, cancellationToken);

        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                "NOT_FOUND" => NotFound(new { message = result.ErrorMessage, code = result.ErrorCode }),
                "NOT_PENDING" => BadRequest(new { message = result.ErrorMessage, code = result.ErrorCode }),
                _ => BadRequest(new { message = result.ErrorMessage, code = result.ErrorCode }),
            };
        }

        return Ok(result.Data);
    }
}
