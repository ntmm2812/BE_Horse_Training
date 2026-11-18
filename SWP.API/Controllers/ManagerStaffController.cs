using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP.Application.Common;
using SWP.Application.DTOs.Staff;
using SWP.Application.Services.Interface;
using SWP.Domain.Common;

namespace SWP.API.Controllers;

[ApiController]
[Route("api/manager/staff")]
[Authorize(Roles = RoleNames.ClubManager)]
public class ManagerStaffController : ControllerBase
{
    private readonly IStaffService _staffService;

    public ManagerStaffController(IStaffService staffService)
    {
        _staffService = staffService;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        return Ok(await _staffService.ListAsync(cancellationToken));
    }

    [HttpGet("{userId:int}")]
    public async Task<IActionResult> GetById(int userId, CancellationToken cancellationToken)
    {
        var result = await _staffService.GetByIdAsync(userId, cancellationToken);
        return FromResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] SaveStaffAccountRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _staffService.CreateAsync(request, cancellationToken);
        return FromResult(result, StatusCodes.Status201Created);
    }

    [HttpPut("{userId:int}")]
    public async Task<IActionResult> Update(
        int userId,
        [FromBody] SaveStaffAccountRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _staffService.UpdateAsync(userId, request, cancellationToken);
        return FromResult(result);
    }

    [HttpPatch("{userId:int}/active")]
    public async Task<IActionResult> SetActive(
        int userId,
        [FromBody] SetStaffActiveRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _staffService.SetActiveAsync(userId, request.IsActive, cancellationToken);
        return FromResult(result);
    }

    private IActionResult FromResult<T>(ServiceResult<T> result, int successStatus = StatusCodes.Status200OK)
    {
        if (result.Success)
        {
            return StatusCode(successStatus, result.Data);
        }

        var body = new { message = result.ErrorMessage, code = result.ErrorCode };
        return result.ErrorCode switch
        {
            ServiceErrorCodes.NotFound => NotFound(body),
            ServiceErrorCodes.Conflict => Conflict(body),
            _ => BadRequest(body),
        };
    }
}
