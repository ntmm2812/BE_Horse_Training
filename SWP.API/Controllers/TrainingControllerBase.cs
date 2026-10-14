using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SWP.Application.Common;

namespace SWP.API.Controllers;

/// <summary> Ánh xạ <see cref="ServiceResult{T}"/> sang HTTP cho các controller luồng giáo án. </summary>
public abstract class TrainingControllerBase : ControllerBase
{
    protected IActionResult FromResult<T>(ServiceResult<T> result, int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.Success)
        {
            return StatusCode(successStatusCode, result.Data);
        }

        var body = new
        {
            message = result.ErrorMessage,
            code = result.ErrorCode,
            details = result.Details,
        };

        return result.ErrorCode switch
        {
            ServiceErrorCodes.NotFound => NotFound(body),
            ServiceErrorCodes.InvalidStatus or ServiceErrorCodes.Conflict => Conflict(body),
            ServiceErrorCodes.HorseNotFit => UnprocessableEntity(body),
            _ => BadRequest(body),
        };
    }

    protected IActionResult NoContentFrom(ServiceResult<bool> result) =>
        result.Success ? NoContent() : FromResult(result);

    protected bool TryGetCurrentUserId(out int userId)
    {
        userId = 0;
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return !string.IsNullOrWhiteSpace(sub) && int.TryParse(sub, out userId);
    }

    protected IActionResult UnknownUser() =>
        Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });
}
