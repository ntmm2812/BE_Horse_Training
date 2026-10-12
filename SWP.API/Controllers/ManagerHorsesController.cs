using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP.Application.DTOs.Horses;
using SWP.Application.Services.Interface;
using SWP.Domain.Common;

namespace SWP.API.Controllers;

[ApiController]
[Route("api/manager/horses")]
[Authorize(Roles = RoleNames.ClubManager)]
public class ManagerHorsesController : ControllerBase
{
    private readonly IHorseService _horseService;

    public ManagerHorsesController(IHorseService horseService)
    {
        _horseService = horseService;
    }

    [HttpGet("owners")]
    public async Task<IActionResult> GetOwners(CancellationToken cancellationToken)
    {
        var owners = await _horseService.GetHorseOwnersAsync(cancellationToken);
        return Ok(owners);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveHorseRequestDto request, CancellationToken cancellationToken)
    {
        var (success, data, error) = await _horseService.CreateAsync(request, cancellationToken);
        if (!success)
        {
            return BadRequest(new { message = error });
        }

        return StatusCode(StatusCodes.Status201Created, data);
    }

    [HttpPut("{horseId:int}")]
    public async Task<IActionResult> Update(
        int horseId,
        [FromBody] SaveHorseRequestDto request,
        CancellationToken cancellationToken)
    {
        var (success, data, error) = await _horseService.UpdateAsync(horseId, request, cancellationToken);
        if (!success)
        {
            return error == "Không tìm thấy ngựa."
                ? NotFound(new { message = error })
                : BadRequest(new { message = error });
        }

        return Ok(data);
    }

    [HttpDelete("{horseId:int}")]
    public async Task<IActionResult> Delete(int horseId, CancellationToken cancellationToken)
    {
        var deleted = await _horseService.DeleteAsync(horseId, cancellationToken);
        if (!deleted)
        {
            return NotFound(new { message = "Không tìm thấy ngựa." });
        }

        return NoContent();
    }
}
