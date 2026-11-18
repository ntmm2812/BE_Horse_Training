using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP.Application.Common;
using SWP.Application.DTOs.Approvals;
using SWP.Application.Services.Implement;
using SWP.Application.Services.Interface;
using SWP.Domain.Common;

namespace SWP.API.Controllers;

[ApiController]
[Route("api/approvals")]
[Authorize(Roles = RoleNames.HeadTrainer + "," + RoleNames.HorseOwner + "," + RoleNames.ClubManager)]
public class ApprovalsController(OwnerApprovalService approvals, IHorseRealtimeNotifier realtime) : TrainingControllerBase
{
    private string Role => User.IsInRole(RoleNames.HorseOwner) ? RoleNames.HorseOwner
        : User.IsInRole(RoleNames.ClubManager) ? RoleNames.ClubManager : RoleNames.HeadTrainer;
    private bool IsOwner => Role == RoleNames.HorseOwner;

    [HttpGet("plans")]
    [Authorize(Roles = RoleNames.HeadTrainer + "," + RoleNames.HorseOwner)]
    public async Task<IActionResult> Plans(CancellationToken ct) => !TryGetCurrentUserId(out var uid) ? UnknownUser()
        : Ok(await approvals.ListPlans(uid, IsOwner, ct));

    [HttpGet("plans/{id:int}")]
    [Authorize(Roles = RoleNames.HeadTrainer + "," + RoleNames.HorseOwner)]
    public async Task<IActionResult> Plan(int id, CancellationToken ct) => !TryGetCurrentUserId(out var uid) ? UnknownUser()
        : FromResult(await approvals.GetPlan(id, uid, IsOwner, ct));

    [HttpPost("plans/{id:int}/send")]
    [Authorize(Roles = RoleNames.HeadTrainer)]
    public async Task<IActionResult> Send(int id, CancellationToken ct) => await Notify(await approvals.SendPlan(id, ct), "TapLuyen", ct);

    [HttpPost("plans/{id:int}/decision")]
    [Authorize(Roles = RoleNames.HorseOwner)]
    public async Task<IActionResult> DecidePlan(int id, PlanDecisionRequest request, CancellationToken ct) => !TryGetCurrentUserId(out var uid)
        ? UnknownUser() : await Notify(await approvals.DecidePlan(id, uid, request, ct), "TapLuyen", ct);

    [HttpGet("plans/unread")]
    [Authorize(Roles = RoleNames.HeadTrainer + "," + RoleNames.HorseOwner)]
    public async Task<IActionResult> Unread(CancellationToken ct) => !TryGetCurrentUserId(out var uid) ? UnknownUser()
        : Ok(await approvals.CountPlanNotices(uid, IsOwner, ct));

    [HttpPost("plans/read")]
    [Authorize(Roles = RoleNames.HeadTrainer + "," + RoleNames.HorseOwner)]
    public async Task<IActionResult> Read(ReadPlanNoticesRequest request, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var uid)) return UnknownUser();
        await approvals.ReadPlanNotices(uid, IsOwner, request.PlanId, ct);
        return NoContent();
    }

    [HttpGet("owner-sessions")]
    [Authorize(Roles = RoleNames.HorseOwner)]
    public async Task<IActionResult> Sessions([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var uid)) return UnknownUser();
        if (from == default || to < from || to.DayNumber - from.DayNumber > 366) return BadRequest(new { message = "Khoảng ngày không hợp lệ (tối đa 366 ngày)." });
        return Ok(await approvals.OwnerSessions(uid, from, to, ct));
    }

    [HttpGet("races")]
    public async Task<IActionResult> Races(CancellationToken ct) => !TryGetCurrentUserId(out var uid) ? UnknownUser()
        : Ok(await approvals.ListRaces(uid, Role, ct));

    [HttpGet("races/{id:int}")]
    public async Task<IActionResult> Race(int id, CancellationToken ct) => !TryGetCurrentUserId(out var uid) ? UnknownUser()
        : FromResult(await approvals.GetRace(id, uid, Role, ct));

    [HttpPost("races")]
    [Authorize(Roles = RoleNames.HeadTrainer)]
    public async Task<IActionResult> CreateRace(RaceProposalRequest request, CancellationToken ct) => !TryGetCurrentUserId(out var uid)
        ? UnknownUser() : await Notify(await approvals.CreateRace(uid, request, ct), "ThiDau", ct);

    [HttpPost("races/{id:int}/submit")]
    [Authorize(Roles = RoleNames.HeadTrainer)]
    public Task<IActionResult> Submit(int id, CancellationToken ct) => Act(id, "submit", null, ct);

    [HttpPost("races/{id:int}/withdraw")]
    [Authorize(Roles = RoleNames.HeadTrainer)]
    public Task<IActionResult> Withdraw(int id, CancellationToken ct) => Act(id, "withdraw", null, ct);

    [HttpPost("races/{id:int}/decision")]
    [Authorize(Roles = RoleNames.HorseOwner + "," + RoleNames.ClubManager)]
    public Task<IActionResult> DecideRace(int id, RaceDecisionRequest request, CancellationToken ct) => Act(id, "decision", request, ct);

    [HttpPost("races/{id:int}/seen")]
    [Authorize(Roles = RoleNames.HeadTrainer)]
    public async Task<IActionResult> Seen(int id, CancellationToken ct)
    {
        await approvals.MarkRaceSeen(id, ct);
        return NoContent();
    }

    private async Task<IActionResult> Act(int id, string action, RaceDecisionRequest? request, CancellationToken ct) =>
        !TryGetCurrentUserId(out var uid) ? UnknownUser() : await Notify(await approvals.ActOnRace(id, uid, Role, action, request, ct), "ThiDau", ct);

    private async Task<IActionResult> Notify(ServiceResult<object> result, string kind, CancellationToken ct)
    {
        if (result.Success && result.Data is not null)
        {
            var data = System.Text.Json.JsonSerializer.SerializeToElement(result.Data);
            if (data.TryGetProperty("HorseId", out var horseId)) await realtime.NotifyHorseChangedAsync(horseId.GetInt32(), kind, ct);
        }
        return FromResult(result);
    }
}
