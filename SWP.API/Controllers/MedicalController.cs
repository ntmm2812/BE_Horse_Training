using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using SWP.Application.DTOs.Medical;
using SWP.Application.Services.Implement;
using SWP.Application.Services.Interface;
using SWP.Domain.Common;

namespace SWP.API.Controllers;

public sealed class MedicalExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var error = context.Exception switch
        {
            DbUpdateConcurrencyException => (409, "Dữ liệu đã thay đổi. Vui lòng tải lại."),
            KeyNotFoundException ex => (404, ex.Message),
            ArgumentException ex => (400, ex.Message),
            UnauthorizedAccessException ex => (403, ex.Message),
            InvalidOperationException ex => (409, ex.Message),
            _ => (0, "")
        };
        if (error.Item1 == 0) return;
        context.Result = new ObjectResult(new { message = error.Item2 }) { StatusCode = error.Item1 };
        context.ExceptionHandled = true;
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = RoleNames.Veterinarian)]
[TypeFilter(typeof(MedicalExceptionFilter))]
public class MedicalController : ControllerBase
{
    private readonly IMedicalService _medicalService;

    public MedicalController(IMedicalService medicalService)
    {
        _medicalService = medicalService;
    }

    // ==================== BỆNH ÁN (MEDICALRECORD) ====================

    [HttpGet("incident-reports")]
    public Task<List<IncidentReportDto>> IncidentReports([FromQuery] int? horseId, CancellationToken ct) =>
        _medicalService.GetIncidentsForVetAsync(horseId, ct);

    [HttpGet("records/{recordId:int}/is-latest")]
    public Task<bool> IsLatest(int recordId, CancellationToken ct) => _medicalService.IsLatestMedicalRecordAsync(recordId, ct);

    [HttpPut("horses/{horseId:int}/health")]
    public Task<bool> UpdateHealth(int horseId, UpdateHealthStatusRequestDto request, CancellationToken ct) =>
        _medicalService.UpdateHealthStatusAsync(horseId, request, ct);

    [HttpGet("records")]
    public async Task<IActionResult> GetRecords([FromQuery] int? horseId, CancellationToken cancellationToken)
    {
        var records = await _medicalService.GetMedicalRecordsAsync(horseId, cancellationToken);
        return Ok(records);
    }

    [HttpGet("records/{recordId:int}")]
    public async Task<IActionResult> GetRecordById(int recordId, CancellationToken cancellationToken)
    {
        var record = await _medicalService.GetMedicalRecordByIdAsync(recordId, cancellationToken);
        if (record is null)
        {
            return NotFound(new { message = "Không tìm thấy bệnh án." });
        }
        return Ok(record);
    }

    [HttpGet("records/latest-exam/{horseId:int}")]
    public async Task<IActionResult> GetLatestExamDate(int horseId, CancellationToken cancellationToken)
    {
        var result = await _medicalService.GetLatestExamDateAsync(horseId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("records")]
    public async Task<IActionResult> CreateRecord([FromBody] SaveMedicalRecordRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var vetId = GetCurrentUserId();
            if (vetId <= 0) return Unauthorized();
            var result = await _medicalService.CreateMedicalRecordAsync(request, vetId, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("records/{recordId:int}")]
    public async Task<IActionResult> UpdateRecord(int recordId, [FromBody] SaveMedicalRecordRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var vetId = GetCurrentUserId();
            if (vetId <= 0) return Unauthorized();
            var result = await _medicalService.UpdateMedicalRecordAsync(recordId, request, vetId, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ==================== LỆNH KHÓA HUẤN LUYỆN (TRAININGLOCK) ====================

    [HttpGet("training-locks")]
    public async Task<IActionResult> GetTrainingLocks(CancellationToken cancellationToken)
    {
        var locks = await _medicalService.GetTrainingLocksAsync(cancellationToken);
        return Ok(locks);
    }

    [HttpGet("training-locks/active")]
    public async Task<IActionResult> GetActiveTrainingLocks(CancellationToken cancellationToken)
    {
        var locks = await _medicalService.GetActiveTrainingLocksAsync(cancellationToken);
        return Ok(locks);
    }

    [HttpPost("training-locks")]
    public async Task<IActionResult> CreateTrainingLock([FromBody] CreateTrainingLockRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var vetId = GetCurrentUserId();
            if (vetId <= 0) return Unauthorized();
            var result = await _medicalService.CreateTrainingLockAsync(request, vetId, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("training-locks/{lockId:int}/release")]
    public async Task<IActionResult> ReleaseTrainingLock(int lockId, [FromBody] ReleaseTrainingLockRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var vetId = GetCurrentUserId();
            if (vetId <= 0) return Unauthorized();
            var result = await _medicalService.ReleaseTrainingLockAsync(lockId, request, vetId, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // ==================== LỊCH TIÊM PHÒNG / TẨY GIUN (VACCINATIONSCHEDULE) ====================

    [HttpGet("vaccinations")]
    public async Task<IActionResult> GetVaccinations([FromQuery] int? horseId, CancellationToken cancellationToken)
    {
        var list = await _medicalService.GetVaccinationsAsync(horseId, cancellationToken);
        return Ok(list);
    }

    [HttpGet("vaccinations/{scheduleId:int}")]
    public async Task<IActionResult> GetVaccinationById(int scheduleId, CancellationToken cancellationToken)
    {
        var item = await _medicalService.GetVaccinationByIdAsync(scheduleId, cancellationToken);
        if (item is null)
        {
            return NotFound(new { message = "Không tìm thấy lịch tiêm." });
        }
        return Ok(item);
    }

    [HttpPost("vaccinations")]
    public async Task<IActionResult> CreateVaccination([FromBody] SaveVaccinationRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _medicalService.CreateVaccinationAsync(request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("vaccinations/{scheduleId:int}")]
    public async Task<IActionResult> UpdateVaccination(int scheduleId, [FromBody] SaveVaccinationRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _medicalService.UpdateVaccinationAsync(scheduleId, request, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("vaccinations/{scheduleId:int}")]
    public async Task<IActionResult> DeleteVaccination(int scheduleId, CancellationToken cancellationToken)
    {
        try
        {
            await _medicalService.DeleteVaccinationAsync(scheduleId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("vaccinations/{scheduleId:int}/mark-done")]
    public async Task<IActionResult> MarkVaccinated(int scheduleId, [FromBody] MarkVaccinatedRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _medicalService.MarkVaccinatedAsync(scheduleId, request?.NextDate, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // ==================== BÁC SĨ THÚ Y (VETS) ====================

    [HttpGet("vets")]
    public async Task<IActionResult> GetVets(CancellationToken cancellationToken)
    {
        var vets = await _medicalService.GetVetsAsync(cancellationToken);
        return Ok(vets);
    }

    private int GetCurrentUserId()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!string.IsNullOrWhiteSpace(sub) && int.TryParse(sub, out var userId) && userId > 0)
        {
            return userId;
        }

        return 0;
    }
}
