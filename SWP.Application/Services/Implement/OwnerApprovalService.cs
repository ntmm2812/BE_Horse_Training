using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SWP.Application.Common;
using SWP.Application.DTOs.Approvals;
using SWP.Application.Services.Interface;
using SWP.Domain.Common;
using SWP.Domain.Models.Entity;
using SWP.Infrastructure.Persistence;

namespace SWP.Application.Services.Implement;

public class OwnerApprovalService(ApplicationDbContext db, IRaceService races, TimeProvider clock)
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    private DateOnly Today => DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
    private static ServiceResult<object> InvalidState(string message) => ServiceResult<object>.Fail(ServiceErrorCodes.InvalidStatus, message);
    private static ServiceResult<object> Missing() => ServiceResult<object>.NotFound("Không tìm thấy yêu cầu duyệt.");
    private static string? NoteError(string? note, bool required) =>
        required && string.IsNullOrWhiteSpace(note) ? "Vui lòng nhập lý do." : (note?.Trim().Length ?? 0) > 300 ? "Lý do tối đa 300 ký tự." : null;

    private IQueryable<TrainingPlan> Plans(int userId, bool owner) => db.TrainingPlans
        .Include(p => p.Horse).Include(p => p.CreatedByUser).Include(p => p.Phases).Include(p => p.TrainingSchedules)
        .Where(p => !owner || (p.Horse.UserId == userId && p.ApprovalStatus != OwnerApprovalStatuses.Draft));

    // Explicit projection: owner responses never contain internal notes, groom reports or trainer-only fields.
    private object PlanRecord(TrainingPlan plan) => new
    {
        plan.PlanId, plan.HorseId, Status = plan.ApprovalStatus, Note = plan.ApprovalNote,
        plan.SentAt, plan.DecidedAt, Version = plan.ApprovalVersion, plan.OwnerSeen, plan.TrainerSeen,
        Snapshot = new
        {
            plan.PlanId, plan.PlanName, plan.HorseId, HorseName = plan.Horse.HorseName,
            TrainerName = plan.CreatedByUser?.FullName ?? "Huấn luyện viên", plan.Objective,
            plan.TrainingType, plan.StartDate, plan.EndDate,
            Phases = plan.Phases.OrderBy(p => p.PhaseOrder).Select(TrainingMapper.ToPhaseDto).ToList(),
            Sessions = plan.TrainingSchedules.Where(s => s.Status != TrainingSessionStatuses.Cancelled)
                .OrderBy(s => s.TrainingDate).ThenBy(s => s.StartTime).Select(s => new
                {
                    SessionId = s.ScheduleId, TrainingDate = DateOnly.FromDateTime(s.TrainingDate),
                    StartTime = s.StartTime?.ToString("HH:mm"), s.SessionTitle, s.TrainingType,
                    s.DistanceKm, s.DurationMinutes, s.Status,
                }).ToList(),
        },
    };

    public async Task<object> ListPlans(int userId, bool owner, CancellationToken ct) =>
        (await Plans(userId, owner).AsNoTracking().OrderByDescending(p => p.SentAt).ToListAsync(ct)).Select(PlanRecord).ToList();

    public async Task<ServiceResult<object>> GetPlan(int id, int userId, bool owner, CancellationToken ct)
    {
        var plan = await Plans(userId, owner).AsNoTracking().FirstOrDefaultAsync(p => p.PlanId == id, ct);
        return plan is null ? Missing() : ServiceResult<object>.Ok(PlanRecord(plan));
    }

    public async Task<ServiceResult<object>> SendPlan(int id, CancellationToken ct)
    {
        var plan = await Plans(0, false).FirstOrDefaultAsync(p => p.PlanId == id, ct);
        if (plan is null) return Missing();
        if (plan.PlanStatus is not (TrainingPlanStatuses.Draft or TrainingPlanStatuses.Active)) return InvalidState("Giáo án đã kết thúc.");
        if (plan.ApprovalStatus is not (OwnerApprovalStatuses.Draft or OwnerApprovalStatuses.ChangesRequested)) return InvalidState("Giáo án không ở trạng thái có thể gửi duyệt.");
        if (plan.Phases.Count == 0 || plan.EndDate < Today) return InvalidState("Hãy cập nhật các giai đoạn và thời gian giáo án trước khi gửi.");
        plan.ApprovalStatus = OwnerApprovalStatuses.PendingOwner;
        plan.ApprovalNote = "";
        plan.SentAt = Now;
        plan.DecidedAt = null;
        plan.OwnerSeen = false;
        plan.TrainerSeen = true;
        plan.ApprovalVersion = Guid.NewGuid().ToString();
        await db.SaveChangesAsync(ct);
        return ServiceResult<object>.Ok(PlanRecord(plan));
    }

    public async Task<ServiceResult<object>> DecidePlan(int id, int ownerId, PlanDecisionRequest request, CancellationToken ct)
    {
        var plan = await Plans(ownerId, true).FirstOrDefaultAsync(p => p.PlanId == id, ct);
        if (plan is null) return Missing();
        if (request.Version != plan.ApprovalVersion) return InvalidState("Giáo án đã thay đổi. Vui lòng tải lại trước khi duyệt.");
        if (plan.ApprovalStatus != OwnerApprovalStatuses.PendingOwner || plan.PlanStatus is not (TrainingPlanStatuses.Draft or TrainingPlanStatuses.Active)) return InvalidState("Giáo án không còn chờ duyệt.");
        if (request.Status is not (OwnerApprovalStatuses.Approved or OwnerApprovalStatuses.ChangesRequested or OwnerApprovalStatuses.Rejected)) return ServiceResult<object>.Invalid("Quyết định không hợp lệ.");
        var error = NoteError(request.Note, request.Status != OwnerApprovalStatuses.Approved);
        if (error is not null) return ServiceResult<object>.Invalid(error);
        plan.ApprovalStatus = request.Status;
        plan.ApprovalNote = request.Status == OwnerApprovalStatuses.Approved ? "" : request.Note!.Trim();
        plan.DecidedAt = Now;
        plan.OwnerSeen = true;
        plan.TrainerSeen = false;
        plan.ApprovalVersion = Guid.NewGuid().ToString();
        await db.SaveChangesAsync(ct);
        return ServiceResult<object>.Ok(PlanRecord(plan));
    }

    public async Task<object> CountPlanNotices(int userId, bool owner, CancellationToken ct) =>
        await Plans(userId, owner).CountAsync(p => owner ? !p.OwnerSeen : !p.TrainerSeen, ct);

    public async Task ReadPlanNotices(int userId, bool owner, int? planId, CancellationToken ct)
    {
        // A conditional column-only update cannot overwrite a concurrent send/decision.
        var query = db.TrainingPlans.Where(p => planId == null || p.PlanId == planId);
        if (owner) query = query.Where(p => p.ApprovalStatus != OwnerApprovalStatuses.Draft
            && db.Horses.Any(h => h.HorseId == p.HorseId && h.UserId == userId));
        if (owner) await query.Where(p => !p.OwnerSeen).ExecuteUpdateAsync(s => s.SetProperty(p => p.OwnerSeen, true), ct);
        else await query.Where(p => !p.TrainerSeen).ExecuteUpdateAsync(s => s.SetProperty(p => p.TrainerSeen, true), ct);
    }

    public async Task<object> OwnerSessions(int ownerId, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var plans = await Plans(ownerId, true).AsNoTracking()
            .Where(p => p.ApprovalStatus == OwnerApprovalStatuses.Approved && p.PlanStatus == TrainingPlanStatuses.Active).ToListAsync(ct);
        return plans.SelectMany(p => p.TrainingSchedules
            .Where(s => s.Status is TrainingSessionStatuses.Scheduled or TrainingSessionStatuses.InProgress)
            .Where(s => DateOnly.FromDateTime(s.TrainingDate) >= from && DateOnly.FromDateTime(s.TrainingDate) <= to)
            .Select(s => new
            {
                Key = $"{p.PlanId}-{s.ScheduleId}", p.HorseId, p.PlanId, Date = DateOnly.FromDateTime(s.TrainingDate),
                Time = s.StartTime?.ToString("HH:mm"), Title = s.SessionTitle, s.TrainingType, s.DistanceKm, s.DurationMinutes,
            })).ToList();
    }

    private IQueryable<RaceApproval> Proposals(int userId, string role) => db.RaceApprovals
        .Include(r => r.Horse).Include(r => r.Race).Include(r => r.Trainer)
        .Where(r => role == RoleNames.HeadTrainer || (r.SubmittedAt != null &&
            (role == RoleNames.ClubManager || (role == RoleNames.HorseOwner && r.Horse.UserId == userId))));

    private DateOnly Deadline(Race race) => DateOnly.FromDateTime(race.RegistrationDeadline ?? race.RaceDate!.Value.AddDays(-7));
    private string EffectiveStatus(RaceApproval r) => r.Status is "Draft" or "PendingOwner" or "PendingManager"
        && (Deadline(r.Race) < Today || RaceCatalog.IsClosedRace(r.Race.Status)) ? "Expired" : r.Status;
    private static List<ApprovalHistory> History(RaceApproval r) => JsonSerializer.Deserialize<List<ApprovalHistory>>(r.HistoryJson) ?? [];
    private void AddHistory(RaceApproval r, string step, string action, string by, string? note = null)
    {
        var history = History(r);
        history.Add(new(step, action, by, note?.Trim() ?? "", Now));
        r.HistoryJson = JsonSerializer.Serialize(history);
        r.Version = Guid.NewGuid().ToString();
    }
    private object RaceRecord(RaceApproval r) => new
    {
        r.Id, r.HorseId, HorseName = r.Horse.HorseName, r.RaceId, RaceName = r.Race.RaceName,
        RaceDate = DateOnly.FromDateTime(r.Race.RaceDate!.Value), RaceLocation = r.Race.Location,
        RegistrationDeadline = Deadline(r.Race), r.Race.EntryFee, r.Note, TrainerName = r.Trainer.FullName,
        Status = EffectiveStatus(r), r.RejectedBy, r.RejectReason, r.CreatedAt, r.TrainerSeen,
        History = History(r), r.Version,
    };

    public async Task<object> ListRaces(int userId, string role, CancellationToken ct) =>
        (await Proposals(userId, role).AsNoTracking().OrderByDescending(r => r.CreatedAt).ToListAsync(ct)).Select(RaceRecord).ToList();

    public async Task<ServiceResult<object>> GetRace(int id, int userId, string role, CancellationToken ct)
    {
        var proposal = await Proposals(userId, role).AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct);
        return proposal is null ? Missing() : ServiceResult<object>.Ok(RaceRecord(proposal));
    }

    private async Task<bool> HasSameDay(int horseId, DateTime raceDate, int exceptId, CancellationToken ct)
    {
        var proposals = await db.RaceApprovals.Include(r => r.Race)
            .Where(r => r.HorseId == horseId && r.Id != exceptId).ToListAsync(ct);
        return proposals.Any(r => r.Race.RaceDate?.Date == raceDate.Date && EffectiveStatus(r) is "Draft" or "PendingOwner" or "PendingManager" or "Approved")
            || await db.RaceRegistrations.AnyAsync(r => r.HorseId == horseId && r.Race.RaceDate!.Value.Date == raceDate.Date
                && r.RegistrationStatus != "Cancelled" && r.RegistrationStatus != "Canceled" && r.RegistrationStatus != "Withdrawn" && r.RegistrationStatus != "Rejected", ct);
    }

    public async Task<ServiceResult<object>> CreateRace(int trainerId, RaceProposalRequest request, CancellationToken ct)
    {
        var error = NoteError(request.Note, false);
        if (error is not null) return ServiceResult<object>.Invalid(error);
        // Serialize competing same-horse proposals and race-capacity checks on SQL Server.
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var horse = await db.Horses.FindAsync([request.HorseId], ct);
        var race = await db.Races.FindAsync([request.RaceId], ct);
        if (horse is null || race?.RaceDate is null) return Missing();
        if (Deadline(race) < Today || RaceCatalog.IsClosedRace(race.Status)) return InvalidState("Cuộc đua đã đóng đăng ký.");
        if (await HasSameDay(horse.HorseId, race.RaceDate.Value, 0, ct)) return InvalidState("Ngựa đã có đăng ký trong cùng ngày cuộc đua.");
        var proposal = new RaceApproval { Horse = horse, Race = race, TrainerId = trainerId, Note = request.Note?.Trim() ?? "", CreatedAt = Now };
        db.RaceApprovals.Add(proposal);
        var trainer = await db.Users.FindAsync([trainerId], ct);
        proposal.Trainer = trainer!;
        AddHistory(proposal, "Trainer", "Tạo nháp", trainer!.FullName, request.Note);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return ServiceResult<object>.Ok(RaceRecord(proposal));
    }

    public async Task<ServiceResult<object>> ActOnRace(int id, int userId, string role, string action, RaceDecisionRequest? request, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var r = await Proposals(userId, role).FirstOrDefaultAsync(r => r.Id == id, ct);
        if (r is null) return Missing();
        var user = await db.Users.FindAsync([userId], ct);
        var status = EffectiveStatus(r);
        if (action == "submit")
        {
            if (status != "Draft") return InvalidState("Chỉ gửi được đăng ký Nháp còn hạn.");
            if (await HasSameDay(r.HorseId, r.Race.RaceDate!.Value, r.Id, ct)) return InvalidState("Ngựa đã có đăng ký trong cùng ngày cuộc đua.");
            r.Status = "PendingOwner";
            r.SubmittedAt = Now;
            AddHistory(r, "Trainer", "Gửi duyệt", user!.FullName);
        }
        else if (action == "withdraw")
        {
            if (status is not ("Draft" or "PendingOwner" or "PendingManager")) return InvalidState("Chỉ rút được đăng ký còn hạn chưa duyệt xong.");
            r.Status = "Withdrawn";
            AddHistory(r, "Trainer", "Rút đăng ký", user!.FullName);
        }
        else
        {
            if (request?.Version != r.Version) return InvalidState("Đăng ký đã thay đổi. Vui lòng tải lại trước khi duyệt.");
            var owner = role == RoleNames.HorseOwner;
            if (status != (owner ? "PendingOwner" : "PendingManager")) return InvalidState("Đăng ký không còn chờ bạn duyệt.");
            var error = NoteError(request.Note, !request.Approve);
            if (error is not null) return ServiceResult<object>.Invalid(error);
            if (request.Approve && !owner)
            {
                // RegisterAsync and this approval share the scoped DbContext and transaction: both commit or neither does.
                var entry = await races.RegisterAsync(r.RaceId, r.HorseId, r.TrainerId, ct);
                if (!entry.Success) return ServiceResult<object>.Fail(entry.ErrorCode!, entry.ErrorMessage!);
            }
            r.Status = request.Approve ? owner ? "PendingManager" : "Approved" : "Rejected";
            if (!request.Approve) { r.RejectedBy = owner ? "Owner" : "Manager"; r.RejectReason = request.Note!.Trim(); }
            if (r.Status is "Approved" or "Rejected") r.TrainerSeen = false;
            AddHistory(r, owner ? "Owner" : "Manager", request.Approve ? owner ? "Đồng ý" : "Duyệt" : "Từ chối", user!.FullName, request.Note);
        }
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return ServiceResult<object>.Ok(RaceRecord(r));
    }

    public async Task MarkRaceSeen(int id, CancellationToken ct) => await db.RaceApprovals.Where(r => r.Id == id)
        .ExecuteUpdateAsync(s => s.SetProperty(r => r.TrainerSeen, true), ct);
}
