using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP.Application.Common;
using SWP.Application.DTOs.Care;
using SWP.Application.Services.Interface;
using SWP.Domain.Common;

namespace SWP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SuppliesController : ControllerBase
{
    private readonly ICareService _careService;

    public SuppliesController(ICareService careService)
    {
        _careService = careService;
    }

    [HttpGet]
    [Authorize(Roles = RoleNames.Groom + "," + RoleNames.ClubManager)]
    public async Task<IActionResult> GetSupplies(CancellationToken cancellationToken)
    {
        return Ok(await _careService.GetSuppliesAsync(cancellationToken));
    }

    [HttpGet("{supplyId:int}")]
    [Authorize(Roles = RoleNames.ClubManager)]
    public async Task<IActionResult> GetSupply(int supplyId, CancellationToken cancellationToken)
    {
        var supply = await _careService.GetSupplyAsync(supplyId, cancellationToken);
        if (supply is null)
        {
            return NotFound(new { message = "Không tìm thấy vật tư.", code = ServiceErrorCodes.NotFound });
        }

        return Ok(supply);
    }

    [HttpGet("staff")]
    [Authorize(Roles = RoleNames.ClubManager)]
    public async Task<IActionResult> GetStaff(CancellationToken cancellationToken)
    {
        return Ok(await _careService.GetStaffAsync(cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.ClubManager)]
    public async Task<IActionResult> Create([FromBody] SaveSupplyRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _careService.CreateSupplyAsync(request, cancellationToken);
        if (!result.Success)
        {
            return Error(result);
        }

        return Ok(result.Data);
    }

    [HttpPut("{supplyId:int}")]
    [Authorize(Roles = RoleNames.ClubManager)]
    public async Task<IActionResult> Update(
        int supplyId,
        [FromBody] SaveSupplyRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _careService.UpdateSupplyAsync(supplyId, request, cancellationToken);
        if (!result.Success)
        {
            return Error(result);
        }

        return Ok(result.Data);
    }

    [HttpPost("{supplyId:int}/adjust")]
    [Authorize(Roles = RoleNames.ClubManager)]
    public async Task<IActionResult> Adjust(
        int supplyId,
        [FromBody] AdjustStockRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _careService.AdjustStockAsync(supplyId, request.Delta, cancellationToken);
        if (!result.Success)
        {
            return Error(result);
        }

        return Ok(result.Data);
    }

    [HttpDelete("{supplyId:int}")]
    [Authorize(Roles = RoleNames.ClubManager)]
    public async Task<IActionResult> Delete(int supplyId, CancellationToken cancellationToken)
    {
        var result = await _careService.DeleteSupplyAsync(supplyId, cancellationToken);
        if (!result.Success)
        {
            return Error(result);
        }

        return NoContent();
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
}
