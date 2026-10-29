using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP.Application.Common;
using SWP.Application.DTOs.Veterinary;
using SWP.Application.Services.Interface;
using SWP.Domain.Common;

namespace SWP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VetController : ControllerBase
{
    private readonly IVeterinaryService _veterinaryService;

    public VetController(IVeterinaryService veterinaryService)
    {
        _veterinaryService = veterinaryService;
    }

    [HttpGet("medical-records")]
    [Authorize(Roles = RoleNames.Veterinarian)]
    public async Task<IActionResult> GetRecords([FromQuery] int? horseId, CancellationToken cancellationToken)
    {
        var records = await _veterinaryService.GetRecordsAsync(horseId, null, cancellationToken);
        return Ok(records);
    }

    [HttpGet("medical-records/{recordId:int}")]
    [Authorize(Roles = RoleNames.Veterinarian)]
    public async Task<IActionResult> GetRecord(int recordId, CancellationToken cancellationToken)
    {
        var result = await _veterinaryService.GetRecordAsync(recordId, null, cancellationToken);
        if (!result.Success)
        {
            return Error(result);
        }

        return Ok(result.Data);
    }

    [HttpGet("medical-records/{recordId:int}/is-latest")]
    [Authorize(Roles = RoleNames.Veterinarian)]
    public async Task<IActionResult> IsLatest(int recordId, CancellationToken cancellationToken)
    {
        var result = await _veterinaryService.IsLatestRecordAsync(recordId, cancellationToken);
        if (!result.Success)
        {
            return Error(result);
        }

        return Ok(result.Data);
    }

    [HttpGet("horses/{horseId:int}/latest-exam")]
    [Authorize(Roles = RoleNames.Veterinarian + "," + RoleNames.HeadTrainer + "," + RoleNames.HorseOwner)]
    public async Task<IActionResult> GetLatestExam(int horseId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return UnknownUser();
        }

        var result = await _veterinaryService.GetLatestExamAsync(horseId, OwnerScope(userId), cancellationToken);
        if (!result.Success)
        {
            return Error(result);
        }

        return Ok(result.Data);
    }

    [HttpPost("medical-records")]
    [Authorize(Roles = RoleNames.Veterinarian)]
    public async Task<IActionResult> CreateRecord(
        [FromBody] SaveMedicalRecordRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var vetId))
        {
            return UnknownUser();
        }

        var result = await _veterinaryService.CreateRecordAsync(request, vetId, cancellationToken);
        if (!result.Success)
        {
            return Error(result);
        }

        return StatusCode(StatusCodes.Status201Created, result.Data);
    }

    [HttpPut("medical-records/{recordId:int}")]
    [Authorize(Roles = RoleNames.Veterinarian)]
    public async Task<IActionResult> UpdateRecord(
        int recordId,
        [FromBody] SaveMedicalRecordRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var vetId))
        {
            return UnknownUser();
        }

        var result = await _veterinaryService.UpdateRecordAsync(recordId, request, vetId, cancellationToken);
        if (!result.Success)
        {
            return Error(result);
        }

        return Ok(result.Data);
    }

    [HttpGet("training-locks")]
    [Authorize(Roles = RoleNames.Veterinarian + "," + RoleNames.HeadTrainer + "," + RoleNames.HorseOwner)]
    public async Task<IActionResult> GetLocks([FromQuery] bool activeOnly, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return UnknownUser();
        }

        var locks = await _veterinaryService.GetLocksAsync(OwnerScope(userId), activeOnly, cancellationToken);
        return Ok(locks);
    }

    [HttpPost("training-locks")]
    [Authorize(Roles = RoleNames.Veterinarian)]
    public async Task<IActionResult> CreateLock(
        [FromBody] CreateTrainingLockRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var vetId))
        {
            return UnknownUser();
        }

        var result = await _veterinaryService.CreateLockAsync(request, vetId, cancellationToken);
        if (!result.Success)
        {
            return Error(result);
        }

        return StatusCode(StatusCodes.Status201Created, result.Data);
    }

    [HttpPost("training-locks/{lockId:int}/release")]
    [Authorize(Roles = RoleNames.Veterinarian)]
    public async Task<IActionResult> ReleaseLock(
        int lockId,
        [FromBody] ReleaseTrainingLockRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return UnknownUser();
        }

        var result = await _veterinaryService.ReleaseLockAsync(lockId, request.TrangThai, OwnerScope(userId), cancellationToken);
        if (!result.Success)
        {
            return Error(result);
        }

        return Ok(result.Data);
    }

    [HttpGet("vaccinations")]
    [Authorize(Roles = RoleNames.Veterinarian + "," + RoleNames.HeadTrainer + "," + RoleNames.HorseOwner)]
    public async Task<IActionResult> GetVaccinations([FromQuery] int? horseId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return UnknownUser();
        }

        var items = await _veterinaryService.GetVaccinationsAsync(horseId, OwnerScope(userId), cancellationToken);
        return Ok(items);
    }

    [HttpGet("vaccinations/{scheduleId:int}")]
    [Authorize(Roles = RoleNames.Veterinarian + "," + RoleNames.HeadTrainer + "," + RoleNames.HorseOwner)]
    public async Task<IActionResult> GetVaccination(int scheduleId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return UnknownUser();
        }

        var result = await _veterinaryService.GetVaccinationAsync(scheduleId, OwnerScope(userId), cancellationToken);
        if (!result.Success)
        {
            return Error(result);
        }

        return Ok(result.Data);
    }

    [HttpPost("vaccinations")]
    [Authorize(Roles = RoleNames.Veterinarian)]
    public async Task<IActionResult> CreateVaccination(
        [FromBody] SaveVaccinationRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _veterinaryService.CreateVaccinationAsync(request, cancellationToken);
        if (!result.Success)
        {
            return Error(result);
        }

        return StatusCode(StatusCodes.Status201Created, result.Data);
    }

    [HttpPut("vaccinations/{scheduleId:int}")]
    [Authorize(Roles = RoleNames.Veterinarian)]
    public async Task<IActionResult> UpdateVaccination(
        int scheduleId,
        [FromBody] SaveVaccinationRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _veterinaryService.UpdateVaccinationAsync(scheduleId, request, cancellationToken);
        if (!result.Success)
        {
            return Error(result);
        }

        return Ok(result.Data);
    }

    [HttpDelete("vaccinations/{scheduleId:int}")]
    [Authorize(Roles = RoleNames.Veterinarian)]
    public async Task<IActionResult> DeleteVaccination(int scheduleId, CancellationToken cancellationToken)
    {
        var result = await _veterinaryService.DeleteVaccinationAsync(scheduleId, cancellationToken);
        if (!result.Success)
        {
            return Error(result);
        }

        return NoContent();
    }

    [HttpPost("vaccinations/{scheduleId:int}/complete")]
    [Authorize(Roles = RoleNames.Veterinarian)]
    public async Task<IActionResult> MarkVaccinated(
        int scheduleId,
        [FromBody] MarkVaccinatedRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _veterinaryService.MarkVaccinatedAsync(scheduleId, request.NextDate, cancellationToken);
        if (!result.Success)
        {
            return Error(result);
        }

        return Ok(result.Data);
    }

    [HttpGet("session-vitals")]
    [Authorize(Roles = RoleNames.Veterinarian)]
    public async Task<IActionResult> GetSessionVitals(CancellationToken cancellationToken)
    {
        var vitals = await _veterinaryService.GetRecentSessionVitalsAsync(cancellationToken);
        return Ok(vitals);
    }

    [HttpGet("vets")]
    [Authorize(Roles = RoleNames.Veterinarian)]
    public async Task<IActionResult> GetVets(CancellationToken cancellationToken)
    {
        var vets = await _veterinaryService.GetVetsAsync(cancellationToken);
        return Ok(vets);
    }

    [HttpGet("related-horse-ids")]
    [Authorize(Roles = RoleNames.Veterinarian + "," + RoleNames.HeadTrainer + "," + RoleNames.ClubManager)]
    public async Task<IActionResult> GetRelatedHorseIds(CancellationToken cancellationToken)
    {
        var ids = await _veterinaryService.GetRelatedHorseIdsAsync(cancellationToken);
        return Ok(ids);
    }

    private IActionResult Error<T>(ServiceResult<T> result)
    {
        var body = new
        {
            message = result.ErrorMessage,
            code = result.ErrorCode,
        };

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

    private int? OwnerScope(int userId) =>
        User.IsInRole(RoleNames.HorseOwner)
        && !User.IsInRole(RoleNames.Veterinarian)
        && !User.IsInRole(RoleNames.HeadTrainer)
        && !User.IsInRole(RoleNames.ClubManager)
            ? userId
            : null;

    private bool TryGetCurrentUserId(out int userId)
    {
        userId = 0;
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return !string.IsNullOrWhiteSpace(sub) && int.TryParse(sub, out userId);
    }
}
