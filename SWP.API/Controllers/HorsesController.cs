using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP.Application.Services.Interface;

namespace SWP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HorsesController : ControllerBase
{
    private readonly IHorseService _horseService;

    public HorsesController(IHorseService horseService)
    {
        _horseService = horseService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var horses = await _horseService.GetAllAsync(cancellationToken);
        return Ok(horses);
    }

    [HttpGet("{horseId:int}")]
    public async Task<IActionResult> GetById(int horseId, CancellationToken cancellationToken)
    {
        var horse = await _horseService.GetByIdAsync(horseId, cancellationToken);
        if (horse is null)
        {
            return NotFound(new { message = "Không tìm thấy ngựa." });
        }

        return Ok(horse);
    }
}
