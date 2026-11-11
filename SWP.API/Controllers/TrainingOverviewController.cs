using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP.Domain.Common;
using SWP.Domain.Models.Entity;
using SWP.Infrastructure.Persistence;

namespace SWP.API.Controllers;

// Read adapters for existing profile/report screens; all rows come from the real training tables.
[ApiController]
[Route("api/training-overview")]
[Authorize(Roles = RoleNames.HeadTrainer + "," + RoleNames.ClubManager + "," + RoleNames.Veterinarian + "," + RoleNames.HorseOwner)]
public class TrainingOverviewController(ApplicationDbContext db) : TrainingControllerBase
{
    private bool IsOwner => User.IsInRole(RoleNames.HorseOwner);
    private IQueryable<TrainingPlan> VisiblePlans(int uid) => db.TrainingPlans.AsNoTracking()
        .Where(p => !IsOwner || p.Horse.UserId == uid && p.ApprovalStatus != OwnerApprovalStatuses.Draft);

    [HttpGet("plans")]
    public async Task<IActionResult> Plans([FromQuery] int? horseId, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var uid)) return UnknownUser();
        var plans = await VisiblePlans(uid).Include(p => p.Phases)
            .Where(p => horseId == null || p.HorseId == horseId).OrderByDescending(p => p.PlanId).ToListAsync(ct);
        return Ok(plans.Select(p => new
        {
            PlanID = p.PlanId, HorseID = p.HorseId, p.PlanName,
            CuLy = p.Phases.OrderBy(x => x.PhaseOrder).FirstOrDefault()?.TargetDistanceKm * 1000,
            KhoiLuong = p.Phases.OrderBy(x => x.PhaseOrder).FirstOrDefault()?.SessionDurationMinutes,
            MatSan = p.Phases.OrderBy(x => x.PhaseOrder).FirstOrDefault()?.Surface,
            GiaiDoan = p.Phases.OrderBy(x => x.PhaseOrder).FirstOrDefault()?.PhaseName,
            CreatedBy = p.CreatedByUserId, p.PlanStatus, p.StartDate, p.EndDate,
        }));
    }

    private IQueryable<TrainingSchedule> VisibleSessions(int uid) => db.TrainingSchedules.AsNoTracking()
        .Where(s => !IsOwner || s.Plan.Horse.UserId == uid && s.Plan.ApprovalStatus == OwnerApprovalStatuses.Approved);

    [HttpGet("schedules")]
    public async Task<IActionResult> Schedules([FromQuery] DateOnly? date, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var uid)) return UnknownUser();
        var query = VisibleSessions(uid).Include(s => s.Plan).Where(s => s.Status != TrainingSessionStatuses.Cancelled);
        if (date is not null)
        {
            var start = date.Value.ToDateTime(TimeOnly.MinValue);
            var end = start.AddDays(1);
            query = query.Where(s => s.TrainingDate >= start && s.TrainingDate < end);
        }
        var rows = await query.OrderBy(s => s.TrainingDate).ThenBy(s => s.StartTime).ToListAsync(ct);
        return Ok(rows.Select(s => new
        {
            ScheduleID = s.ScheduleId, HorseID = s.Plan.HorseId, GroomID = s.AssignedToUserId,
            Ngay = DateOnly.FromDateTime(s.TrainingDate), Gio = s.StartTime?.ToString("HH:mm") ?? "",
            s.PlanId, s.SessionTitle, s.Status,
        }));
    }

    [HttpGet("results")]
    public async Task<IActionResult> Results([FromQuery] int? horseId, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var uid)) return UnknownUser();
        var rows = await VisibleSessions(uid).Include(s => s.Plan)
            .Where(s => s.Status == TrainingSessionStatuses.Completed && (horseId == null || s.Plan.HorseId == horseId))
            .OrderByDescending(s => s.TrainingDate).ToListAsync(ct);
        return Ok(rows.Select(s => new
        {
            SessionID = s.ScheduleId, HorseID = s.Plan.HorseId, Ngay = DateOnly.FromDateTime(s.TrainingDate),
            NhipTim = IsOwner ? null : s.AvgHeartRate, VanToc = IsOwner ? null : s.AvgSpeedKmh,
            ChiSoTap = s.PerformanceScore, NhanXet = User.IsInRole(RoleNames.HeadTrainer) ? s.TrainerComment : null,
        }));
    }

    [HttpGet("people")]
    [Authorize(Roles = RoleNames.HeadTrainer + "," + RoleNames.ClubManager)]
    public async Task<IActionResult> People(CancellationToken ct) => Ok(await db.Users.AsNoTracking()
        .Select(u => new { UserID = u.UserId, HoTen = u.FullName, u.Email, RoleName = u.Role.RoleName, IsActive = u.Status == UserStatuses.Active })
        .ToListAsync(ct));
}
