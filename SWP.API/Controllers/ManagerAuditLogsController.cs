using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP.Application.DTOs.Audit;
using SWP.Application.Services.Interface;
using SWP.Domain.Common;

namespace SWP.API.Controllers;

// Club Manager: xem nhật ký thao tác (lọc theo người, khoảng ngày, từ khóa; phân trang).
[ApiController]
[Route("api/manager/audit-logs")]
[Authorize(Roles = RoleNames.ClubManager)]
public class ManagerAuditLogsController(IAuditLogService audit) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] AuditLogQueryDto query, CancellationToken cancellationToken)
    {
        var result = await audit.ListAsync(query, cancellationToken);
        return result.Success
            ? Ok(result.Data)
            : BadRequest(new { message = result.ErrorMessage, code = result.ErrorCode });
    }
}
