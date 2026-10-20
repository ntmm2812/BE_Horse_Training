using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SWP.Application.Common;
using SWP.Application.DTOs.Medical;
using SWP.Domain.Common;
using SWP.Domain.Models.Entity;
using SWP.Infrastructure.Persistence;

namespace SWP.Application.Services.Implement;

public sealed class InjuryWorkflowException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

/// <summary>
/// Một DbContext dùng chung cho hồ sơ, công việc, khóa và lịch sử để ghi nguyên tử.
/// Quyền theo người dùng thật trong DB, không nhận role/người thực hiện từ request body.
/// </summary>
public class InjuryWorkflowService(ApplicationDbContext db, TimeProvider clock)
{
    private static readonly string[] Unresolved = ["Pending", "InProgress", "UnableToComplete"];
    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    private sealed record Actor(int Id, string Role);

    private async Task<Actor> GetActor(int userId, CancellationToken ct, params string[] roles)
    {
        var user = await db.Users.AsNoTracking().Include(u => u.Role).SingleOrDefaultAsync(u => u.UserId == userId, ct);
        if (user is null || !string.Equals(user.Status, UserStatuses.Active, StringComparison.OrdinalIgnoreCase))
            throw new InjuryWorkflowException(403, "Tài khoản không còn hoạt động.");
        var actor = new Actor(userId, RoleMapper.ToRoleId(user.Role.RoleName));
        if (!roles.Contains(actor.Role)) throw new InjuryWorkflowException(403, "Vai trò không được thực hiện thao tác này.");
        return actor;
    }

    private Task<Actor> Vet(int userId, CancellationToken ct) => GetActor(userId, ct, RoleNames.Veterinarian);
    private static string Required(string? value, int max, string label, int min = 5)
    {
        var text = value?.Trim() ?? "";
        if (text.Length < min || text.Length > max)
            throw new InjuryWorkflowException(400, $"{label} phải có từ {min} đến {max} ký tự.");
        return text;
    }

    private static void CheckVersion(int actual, int expected)
    {
        if (actual != expected) throw new InjuryWorkflowException(409, "Dữ liệu đã thay đổi. Hãy tải lại trước khi thao tác.");
    }

    private static void Page(int skip, int take)
    {
        if (skip < 0 || take < 1 || take > 100) throw new InjuryWorkflowException(400, "skip phải >= 0; take từ 1 đến 100.");
    }

    private DateTime? ReviewDate(DateTimeOffset? value)
    {
        if (value.HasValue && value.Value.UtcDateTime <= Now)
            throw new InjuryWorkflowException(400, "Hạn tái khám phải ở tương lai.");
        return value?.UtcDateTime;
    }

    private async Task<T> Transaction<T>(Func<Task<T>> operation, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var result = await operation();
        await tx.CommitAsync(ct);
        return result;
    }

    private async Task<InjuryCase> OpenCase(int id, int? version, CancellationToken ct)
    {
        var injury = await db.InjuryCases.Include(c => c.Horse).SingleOrDefaultAsync(c => c.InjuryCaseId == id, ct)
            ?? throw new InjuryWorkflowException(404, "Không tìm thấy hồ sơ chấn thương.");
        if (version.HasValue) CheckVersion(injury.Version, version.Value);
        if (injury.Status == "Closed") throw new InjuryWorkflowException(409, "Hồ sơ đã đóng; hãy tạo hồ sơ mới nếu có đợt chấn thương mới.");
        return injury;
    }

    private void Event(InjuryCase injury, Actor actor, string kind, object details, int? recordId = null)
    {
        db.InjuryCaseEvents.Add(new InjuryCaseEvent
        {
            InjuryCaseId = injury.InjuryCaseId, ActorUserId = actor.Id, Kind = kind,
            DetailsJson = JsonSerializer.Serialize(details), MedicalRecordId = recordId, CreatedAt = Now
        });
    }

    private async Task Save(InjuryCase injury, Actor actor, string kind, object details, CancellationToken ct, int? recordId = null)
    {
        injury.Version++;
        injury.UpdatedAt = Now;
        Event(injury, actor, kind, details, recordId);
        await db.SaveChangesAsync(ct);
    }

    private static DateTime Utc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
    private static DateTime? Utc(DateTime? value) => value.HasValue ? Utc(value.Value) : null;
    private InjuryCaseDto CaseDto(InjuryCase c, Actor actor) => new(c.InjuryCaseId, c.HorseId, c.Horse.HorseName,
        c.Title, c.BodyRegion, c.Side, c.Severity, c.Status, c.OwnerSummary,
        actor.Role == RoleNames.HorseOwner ? null : c.TrainingGuidance,
        Utc(c.CreatedAt), Utc(c.ReviewDueAt), Utc(c.ClosedAt), c.Status != "Closed" && c.ReviewDueAt < Now, c.Version);

    public async Task<PageDto<InjuryCaseDto>> ListCases(int userId, int? horseId, string? status, int skip, int take, CancellationToken ct)
    {
        var actor = await GetActor(userId, ct, RoleNames.Veterinarian, RoleNames.HeadTrainer, RoleNames.ClubManager, RoleNames.HorseOwner);
        Page(skip, take);
        var query = db.InjuryCases.AsNoTracking().Include(c => c.Horse).AsQueryable();
        if (actor.Role == RoleNames.HorseOwner) query = query.Where(c => c.Horse.UserId == actor.Id);
        if (horseId.HasValue) query = query.Where(c => c.HorseId == horseId);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(c => c.Status == status);
        var total = await query.CountAsync(ct);
        var cases = await query.OrderByDescending(c => c.InjuryCaseId).Skip(skip).Take(take).ToListAsync(ct);
        return new(cases.Select(c => CaseDto(c, actor)).ToList(), total, skip, take);
    }

    public async Task<InjuryCaseDto> GetCase(int userId, int id, CancellationToken ct)
    {
        var actor = await GetActor(userId, ct, RoleNames.Veterinarian, RoleNames.HeadTrainer, RoleNames.ClubManager, RoleNames.HorseOwner);
        var query = db.InjuryCases.AsNoTracking().Include(c => c.Horse).Where(c => c.InjuryCaseId == id);
        if (actor.Role == RoleNames.HorseOwner) query = query.Where(c => c.Horse.UserId == actor.Id);
        var injury = await query.SingleOrDefaultAsync(ct) ?? throw new InjuryWorkflowException(404, "Không tìm thấy hồ sơ.");
        return CaseDto(injury, actor);
    }

    public async Task<PageDto<InjuryEventDto>> Timeline(int userId, int id, int skip, int take, CancellationToken ct)
    {
        await Vet(userId, ct);
        Page(skip, take);
        if (!await db.InjuryCases.AnyAsync(c => c.InjuryCaseId == id, ct)) throw new InjuryWorkflowException(404, "Không tìm thấy hồ sơ.");
        var query = db.InjuryCaseEvents.AsNoTracking().Where(e => e.InjuryCaseId == id);
        var count = await query.CountAsync(ct);
        var items = await query.OrderByDescending(e => e.InjuryCaseEventId).Skip(skip).Take(take).ToListAsync(ct);
        return new(items.Select(e => new InjuryEventDto(e.InjuryCaseEventId, e.Kind, e.ActorUserId, Utc(e.CreatedAt), e.DetailsJson, e.MedicalRecordId)).ToList(), count, skip, take);
    }

    public Task<InjuryCaseDto> CreateCase(int userId, OpenInjuryCaseRequest request, CancellationToken ct) => Transaction(async () =>
    {
        var actor = await Vet(userId, ct);
        var horse = await db.Horses.SingleOrDefaultAsync(h => h.HorseId == request.HorseId, ct)
            ?? throw new InjuryWorkflowException(404, "Không tìm thấy ngựa.");
        if (!new[] { "Left", "Right", "Bilateral", "Unspecified" }.Contains(request.Side)
            || !new[] { "Mild", "Moderate", "Severe" }.Contains(request.Severity))
            throw new InjuryWorkflowException(400, "Bên hoặc mức độ chấn thương không hợp lệ.");
        var notes = Required(request.ClinicalNotes, 4000, "Ghi nhận lâm sàng");
        var injury = new InjuryCase
        {
            Horse = horse, HorseId = horse.HorseId, CreatedByUserId = actor.Id,
            Title = Required(request.Title, 150, "Tên hồ sơ"), BodyRegion = Required(request.BodyRegion, 100, "Vị trí", 2),
            Side = request.Side, Severity = request.Severity,
            OwnerSummary = Required(request.OwnerSummary, 1000, "Tóm tắt cho chủ ngựa"),
            TrainingGuidance = Required(request.TrainingGuidance, 1000, "Hướng dẫn cho HLV"),
            ReviewDueAt = ReviewDate(request.ReviewDueAt), CreatedAt = Now, UpdatedAt = Now
        };
        db.InjuryCases.Add(injury);
        await db.SaveChangesAsync(ct);
        Event(injury, actor, "CaseOpened", new { notes, injury.Title, injury.BodyRegion, injury.Side, injury.Severity, injury.OwnerSummary, injury.TrainingGuidance });
        await db.SaveChangesAsync(ct);
        return CaseDto(injury, actor);
    }, ct);

    public Task<InjuryCaseDto> UpdateSummary(int userId, int id, UpdateCaseSummaryRequest request, CancellationToken ct) => Transaction(async () =>
    {
        var actor = await Vet(userId, ct);
        var injury = await OpenCase(id, request.Version, ct);
        var before = new { injury.OwnerSummary, injury.TrainingGuidance, injury.ReviewDueAt };
        injury.OwnerSummary = Required(request.OwnerSummary, 1000, "Tóm tắt cho chủ ngựa");
        injury.TrainingGuidance = Required(request.TrainingGuidance, 1000, "Hướng dẫn cho HLV");
        injury.ReviewDueAt = ReviewDate(request.ReviewDueAt);
        await Save(injury, actor, "SummaryUpdated", new { before, injury.OwnerSummary, injury.TrainingGuidance,
            injury.ReviewDueAt, reason = Required(request.Reason, 1000, "Lý do") }, ct);
        return CaseDto(injury, actor);
    }, ct);

    public Task<InjuryCaseDto> AddNote(int userId, int id, CaseNoteRequest request, CancellationToken ct) => Transaction(async () =>
    {
        var actor = await Vet(userId, ct);
        var injury = await OpenCase(id, request.Version, ct);
        await Save(injury, actor, "ClinicalNoteAdded", new { note = Required(request.Note, 4000, "Ghi chú") }, ct);
        return CaseDto(injury, actor);
    }, ct);

    public Task<InjuryCaseDto> LinkExam(int userId, int id, LinkCaseExamRequest request, CancellationToken ct) => Transaction(async () =>
    {
        var actor = await Vet(userId, ct);
        var injury = await OpenCase(id, request.Version, ct);
        var record = await db.MedicalRecords.AsNoTracking().SingleOrDefaultAsync(r => r.MedicalRecordId == request.MedicalRecordId, ct);
        if (record is null || record.HorseId != injury.HorseId) throw new InjuryWorkflowException(400, "Bệnh án phải thuộc đúng ngựa.");
        if (await db.InjuryCaseEvents.AnyAsync(e => e.InjuryCaseId == id && e.MedicalRecordId == record.MedicalRecordId, ct))
            throw new InjuryWorkflowException(409, "Bệnh án đã được gắn vào hồ sơ.");
        // Lưu bản chụp, không phụ thuộc bệnh án nguồn bị sửa về sau.
        await Save(injury, actor, "ExamLinked", new { record.MedicalRecordId, record.RecordDate, record.CreateBy,
            record.Diagnosis, record.Symptoms, record.Treatment, record.Medication, record.Dosage, record.FollowUpDate,
            record.ViTriChanThuong, record.Status }, ct, record.MedicalRecordId);
        return CaseDto(injury, actor);
    }, ct);

    public Task<InjuryCaseDto> ChangeStatus(int userId, int id, CaseStatusRequest request, CancellationToken ct) => Transaction(async () =>
    {
        var actor = await Vet(userId, ct);
        var injury = await OpenCase(id, request.Version, ct);
        var allowed = injury.Status switch
        {
            "Open" => new[] { "Treating", "Monitoring", "Closed" },
            "Treating" => new[] { "Monitoring", "Closed" },
            "Monitoring" => new[] { "Treating", "Closed" },
            _ => Array.Empty<string>()
        };
        if (!allowed.Contains(request.Status)) throw new InjuryWorkflowException(409, "Chuyển trạng thái hồ sơ không hợp lệ.");
        if (request.Status == "Closed")
        {
            if (await db.CareTasks.AnyAsync(t => t.InjuryCaseId == id && Unresolved.Contains(t.Status), ct))
                throw new InjuryWorkflowException(409, "Còn công việc chưa được xử lý; hoàn thành hoặc hủy có lý do trước khi đóng hồ sơ.");
            if (await db.TrainingLocks.AnyAsync(l => l.InjuryCaseId == id && l.NgayGo == null, ct))
                throw new InjuryWorkflowException(409, "Còn khóa tập của hồ sơ; cần tái khám và mở khóa trước.");
            var exam = await db.MedicalRecords.AsNoTracking().SingleOrDefaultAsync(r => r.MedicalRecordId == request.ReexamRecordId, ct);
            if (exam is null || exam.HorseId != injury.HorseId || exam.Status != "Completed"
                || exam.RecordDate.Date < injury.CreatedAt.Date || exam.RecordDate.Date > Now.Date
                || await db.MedicalRecords.AnyAsync(r => r.HorseId == injury.HorseId &&
                    (r.RecordDate > exam.RecordDate || (r.RecordDate == exam.RecordDate && r.MedicalRecordId > exam.MedicalRecordId)), ct)
                || !await db.InjuryCaseEvents.AnyAsync(e => e.InjuryCaseId == id && e.MedicalRecordId == exam.MedicalRecordId, ct))
                throw new InjuryWorkflowException(400, "Cần bệnh án tái khám hoàn tất mới nhất, đã gắn vào hồ sơ, từ ngày mở hồ sơ đến hôm nay.");
            injury.ClosedAt = Now;
        }
        var before = injury.Status;
        injury.Status = request.Status;
        await Save(injury, actor, "StatusChanged", new { before, after = injury.Status, request.ReexamRecordId,
            reason = Required(request.Reason, 1000, "Lý do") }, ct);
        return CaseDto(injury, actor);
    }, ct);

    public Task<TrainingLockDto> LockTraining(int userId, int id, CaseLockRequest request, CancellationToken ct) => Transaction(async () =>
    {
        var actor = await Vet(userId, ct);
        var injury = await OpenCase(id, request.Version, ct);
        if (await db.TrainingLocks.AnyAsync(l => l.InjuryCaseId == id && l.NgayGo == null, ct))
            throw new InjuryWorkflowException(409, "Hồ sơ đã có khóa tập còn hiệu lực.");
        var trainingLock = new TrainingLock { InjuryCaseId = id, HorseId = injury.HorseId, VetId = actor.Id,
            NgayDat = DateOnly.FromDateTime(Now), LyDo = Required(request.Reason, 200, "Lý do khóa") };
        db.TrainingLocks.Add(trainingLock);
        var health = await db.HorseHealthStatuses.SingleOrDefaultAsync(h => h.HorseId == injury.HorseId, ct);
        if (health is null)
        {
            health = new HorseHealthStatus { HorseId = injury.HorseId };
            db.HorseHealthStatuses.Add(health);
        }
        if (health.TrangThai != HorseHealthStatuses.CachLy)
        {
            health.TrangThai = HorseHealthStatuses.ChanThuong;
            health.NgayCapNhat = Now;
        }
        await db.SaveChangesAsync(ct);
        await Save(injury, actor, "TrainingLocked", new { trainingLock.LockId, trainingLock.LyDo }, ct);
        return MedicalMapper.ToDto(trainingLock);
    }, ct);

    private async Task<User> Groom(int id, CancellationToken ct)
    {
        var user = await db.Users.Include(u => u.Role).SingleOrDefaultAsync(u => u.UserId == id, ct);
        if (user is null || RoleMapper.ToRoleId(user.Role.RoleName) != RoleNames.Groom
            || !string.Equals(user.Status, UserStatuses.Active, StringComparison.OrdinalIgnoreCase))
            throw new InjuryWorkflowException(400, "Chỉ giao việc cho nhân viên chăm sóc đang hoạt động.");
        return user;
    }

    private DateTime Due(DateTimeOffset date)
    {
        if (date.UtcDateTime <= Now) throw new InjuryWorkflowException(400, "Hạn công việc phải ở tương lai.");
        return date.UtcDateTime;
    }

    private MedicalCareDto TaskDto(CareTask t, Actor actor)
    {
        var overdue = Unresolved.Contains(t.Status) && t.ScheduledAt < Now;
        var clinical = actor.Role != RoleNames.ClubManager;
        return new(t.CareTaskId, t.InjuryCaseId!.Value, t.HorseId, t.Horse.HorseName, t.TaskName, clinical ? t.Description : null,
            t.UserId, t.User?.FullName, t.Status, t.Priority, Utc(t.ScheduledAt), Utc(t.CompletedAt),
            clinical ? t.CompletionNotes : null, overdue, overdue ? Math.Round((Now - t.ScheduledAt!.Value).TotalHours, 2) : 0, t.Version);
    }

    public Task<MedicalCareDto> AssignTask(int userId, int id, AssignMedicalCareRequest request, CancellationToken ct) => Transaction(async () =>
    {
        var actor = await Vet(userId, ct);
        var injury = await OpenCase(id, request.Version, ct);
        var groom = await Groom(request.GroomId, ct);
        if (!new[] { "Normal", "Urgent" }.Contains(request.Priority)) throw new InjuryWorkflowException(400, "Mức ưu tiên không hợp lệ.");
        var task = new CareTask { InjuryCaseId = id, HorseId = injury.HorseId, Horse = injury.Horse, User = groom, UserId = groom.UserId,
            AssignedByUserId = actor.Id, TaskName = Required(request.Title, 150, "Tên công việc"),
            Description = Required(request.Instructions, 1000, "Hướng dẫn chăm sóc"), TaskType = "Medical",
            Status = "Pending", ScheduledAt = Due(request.DueAt), Priority = request.Priority, CreatedAt = Now };
        db.CareTasks.Add(task);
        await db.SaveChangesAsync(ct);
        await Save(injury, actor, "TaskAssigned", new { task.CareTaskId, task.UserId, task.TaskName, task.Description, task.ScheduledAt, task.Priority }, ct);
        return TaskDto(task, actor);
    }, ct);

    private async Task<CareTask> TaskForActor(Actor actor, int id, int version, CancellationToken ct)
    {
        var query = db.CareTasks.Include(t => t.User).Include(t => t.Horse).Where(t => t.CareTaskId == id && t.InjuryCaseId != null);
        if (actor.Role == RoleNames.Groom) query = query.Where(t => t.UserId == actor.Id);
        var task = await query.SingleOrDefaultAsync(ct) ?? throw new InjuryWorkflowException(404, "Không tìm thấy công việc được phân công.");
        CheckVersion(task.Version, version);
        return task;
    }

    public Task<MedicalCareDto> ExecuteTask(int userId, int id, string action, CareResultRequest request, CancellationToken ct) => Transaction(async () =>
    {
        var actor = await GetActor(userId, ct, action == "cancel" ? RoleNames.Veterinarian : RoleNames.Groom);
        var task = await TaskForActor(actor, id, request.Version, ct);
        var injury = await OpenCase(task.InjuryCaseId!.Value, null, ct);
        var before = task.Status;
        var status = action switch
        {
            "start" when task.Status == "Pending" => "InProgress",
            "complete" when task.Status == "InProgress" => "Completed",
            "unable" when task.Status is "Pending" or "InProgress" => "UnableToComplete",
            "cancel" when Unresolved.Contains(task.Status) => "Cancelled",
            _ => throw new InjuryWorkflowException(409, "Trạng thái công việc không cho phép thao tác này.")
        };
        var notes = action == "start" ? null : Required(request.Notes, 1000, "Kết quả hoặc lý do");
        task.Status = status;
        task.CompletionNotes = notes;
        task.CompletedAt = status is "Completed" or "Cancelled" ? Now : null;
        task.Version++;
        await Save(injury, actor, "Task" + status, new { task.CareTaskId, before, after = status, notes }, ct);
        return TaskDto(task, actor);
    }, ct);

    public Task<MedicalCareDto> ReassignTask(int userId, int id, ReassignCareRequest request, CancellationToken ct) => Transaction(async () =>
    {
        var actor = await Vet(userId, ct);
        var task = await TaskForActor(actor, id, request.Version, ct);
        var injury = await OpenCase(task.InjuryCaseId!.Value, null, ct);
        if (task.Status is not ("Pending" or "UnableToComplete")) throw new InjuryWorkflowException(409, "Chỉ giao lại việc chưa bắt đầu hoặc đã báo không thực hiện được.");
        var before = new { task.UserId, task.Status, task.ScheduledAt, task.CompletionNotes };
        task.User = await Groom(request.GroomId, ct);
        task.UserId = task.User.UserId;
        task.AssignedByUserId = actor.Id;
        task.ScheduledAt = Due(request.DueAt);
        task.Status = "Pending";
        task.CompletionNotes = null;
        task.CompletedAt = null;
        task.Version++;
        await Save(injury, actor, "TaskReassigned", new { task.CareTaskId, before, task.UserId, task.ScheduledAt,
            reason = Required(request.Notes, 1000, "Lý do giao lại") }, ct);
        return TaskDto(task, actor);
    }, ct);

    public async Task<PageDto<MedicalCareDto>> ListTasks(int userId, int? caseId, bool overdueOnly, int skip, int take, CancellationToken ct)
    {
        var actor = await GetActor(userId, ct, RoleNames.Veterinarian, RoleNames.ClubManager, RoleNames.Groom);
        Page(skip, take);
        var query = db.CareTasks.AsNoTracking().Include(t => t.User).Include(t => t.Horse).Where(t => t.InjuryCaseId != null);
        if (actor.Role == RoleNames.Groom) query = query.Where(t => t.UserId == actor.Id);
        if (caseId.HasValue) query = query.Where(t => t.InjuryCaseId == caseId);
        if (overdueOnly) query = query.Where(t => Unresolved.Contains(t.Status) && t.ScheduledAt < Now);
        var count = await query.CountAsync(ct);
        var tasks = await query.OrderBy(t => t.ScheduledAt).ThenBy(t => t.CareTaskId).Skip(skip).Take(take).ToListAsync(ct);
        return new(tasks.Select(t => TaskDto(t, actor)).ToList(), count, skip, take);
    }

    public async Task<CareOverviewDto> Overview(int userId, CancellationToken ct)
    {
        await GetActor(userId, ct, RoleNames.Veterinarian, RoleNames.ClubManager);
        var query = db.CareTasks.Where(t => t.InjuryCaseId != null);
        return new(await query.CountAsync(t => t.Status == "Pending", ct),
            await query.CountAsync(t => t.Status == "InProgress", ct),
            await query.CountAsync(t => t.Status == "UnableToComplete", ct),
            await query.CountAsync(t => Unresolved.Contains(t.Status) && t.ScheduledAt < Now, ct),
            await db.InjuryCases.CountAsync(c => c.Status != "Closed" && c.ReviewDueAt < Now, ct));
    }

    public async Task<IReadOnlyList<AssignableGroomDto>> AssignableGrooms(int userId, CancellationToken ct)
    {
        await Vet(userId, ct);
        var users = await db.Users.AsNoTracking().Include(u => u.Role)
            .Where(u => u.Status.ToLower() == UserStatuses.Active.ToLower()).OrderBy(u => u.FullName).ToListAsync(ct);
        return users.Where(u => RoleMapper.ToRoleId(u.Role.RoleName) == RoleNames.Groom)
            .Select(u => new AssignableGroomDto(u.UserId, u.FullName)).ToList();
    }
}
