using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP.Application.DTOs.Audit;
using SWP.Application.Services.Interface;

namespace SWP.API.Controllers;

// Ghi nhật ký thao tác: mọi tài khoản đã đăng nhập; người thực hiện luôn lấy từ JWT.
[ApiController]
[Route("api/audit-logs")]
[Authorize]
public class AuditLogsController(IAuditLogService audit) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Add([FromBody] CreateAuditLogRequestDto request, CancellationToken cancellationToken)
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(sub, out var userId))
        {
            return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });
        }

        var result = await audit.AddAsync(userId, request, cancellationToken);
        return result.Success
            ? NoContent()
            : BadRequest(new { message = result.ErrorMessage, code = result.ErrorCode });
    }
}
