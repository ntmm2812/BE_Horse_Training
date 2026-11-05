using System.IdentityModel.Tokens.Jwt;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP.Infrastructure.Persistence;
using SWP.Application.Services.Interface;
using SWP.Domain.Common;

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

    [HttpGet("health-statuses")]
    public async Task<IActionResult> GetHealthStatuses(CancellationToken cancellationToken)
    {
        if (IsHorseOwnerOnly())
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });
            }

            var ownerStatuses = await _horseService.GetHealthStatusesForOwnerAsync(userId, cancellationToken);
            return Ok(ownerStatuses);
        }

        var statuses = await _horseService.GetHealthStatusesAsync(cancellationToken);
        return Ok(statuses);
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

    // Shared operational data only; clinical notes stay under the veterinarian API.
    [HttpGet("medical-overview")]
    [Authorize(Roles = RoleNames.HeadTrainer + "," + RoleNames.HorseOwner + "," + RoleNames.ClubManager + "," + RoleNames.Veterinarian)]
    public async Task<IActionResult> MedicalOverview([FromServices] ApplicationDbContext db,
        [FromQuery] int? horseId, CancellationToken ct)
    {
        var horses = db.Horses.AsNoTracking();
        if (IsHorseOwnerOnly())
        {
            if (!TryGetCurrentUserId(out var ownerId)) return Unauthorized();
            horses = horses.Where(h => h.UserId == ownerId);
        }
        if (horseId.HasValue) horses = horses.Where(h => h.HorseId == horseId.Value);
        var ids = horses.Select(h => h.HorseId);
        var locks = await db.TrainingLocks.AsNoTracking().Where(l => ids.Contains(l.HorseId) && l.NgayGo == null)
            .Select(l => new { LockID = l.LockId, HorseID = l.HorseId, l.NgayDat, l.NgayGo }).ToListAsync(ct);
        var vaccinations = await db.VaccinationSchedules.AsNoTracking().Where(v => ids.Contains(v.HorseId))
            .Select(v => new { ScheduleID = v.ScheduleId, HorseID = v.HorseId, v.Loai, v.NgayHen, v.TrangThai }).ToListAsync(ct);
        var latestExams = await db.MedicalRecords.AsNoTracking().Where(r => ids.Contains(r.HorseId))
            .GroupBy(r => r.HorseId).Select(g => new { HorseID = g.Key, Ngay = g.Max(r => r.RecordDate) }).ToListAsync(ct);
        var sessions = await db.TrainingSchedules.AsNoTracking().Where(t => ids.Contains(t.Plan.HorseId))
            .Select(t => new { SessionID = t.ScheduleId, ScheduleID = t.ScheduleId, HorseID = t.Plan.HorseId,
                Ngay = t.TrainingDate, Gio = t.StartTime, NhipTim = t.MaxHeartRate ?? t.AvgHeartRate,
                t.Status, TenBaiTap = t.SessionTitle }).ToListAsync(ct);
        var races = await db.RaceRegistrations.AsNoTracking().Where(r => ids.Contains(r.HorseId))
            .Select(r => new { HorseID = r.HorseId, Ngay = r.Race.RaceDate }).ToListAsync(ct);
        var usedHorseIds = await horses.Where(h => h.TrainingPlans.Any() || h.CareTasks.Any()
            || h.MedicalRecords.Any() || h.TrainingLocks.Any() || h.VaccinationSchedules.Any()
            || h.RaceRegistrations.Any() || h.StableAssignments.Any() || h.HealthMetrics.Any()
            || h.FeedingPlans.Any() || db.HorseHealthStatuses.Any(x => x.HorseId == h.HorseId))
            .Select(h => h.HorseId).ToListAsync(ct);
        // Match the existing FE data shapes without changing global JSON settings:
        // HorseID/LockID, calendar dates yyyy-MM-dd, and local clock times HH:mm.
        return new JsonResult(new
        {
            locks,
            vaccinations,
            latestExams = latestExams.Select(r => new
            {
                r.HorseID, Ngay = r.Ngay.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            }),
            sessions = sessions.Select(s => new
            {
                s.SessionID, s.ScheduleID, s.HorseID,
                Ngay = s.Ngay.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Gio = s.Gio?.ToString("HH:mm", CultureInfo.InvariantCulture) ?? "",
                s.NhipTim, s.Status, s.TenBaiTap
            }),
            races = races.Select(r => new
            {
                r.HorseID, Ngay = r.Ngay?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? ""
            }),
            usedHorseIds
        }, new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNamingPolicy = null });
    }

    private bool IsHorseOwnerOnly() =>
        User.IsInRole(RoleNames.HorseOwner) && !User.IsInRole(RoleNames.ClubManager);

    private bool TryGetCurrentUserId(out int userId)
    {
        userId = 0;
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return !string.IsNullOrWhiteSpace(sub) && int.TryParse(sub, out userId);
    }
}
