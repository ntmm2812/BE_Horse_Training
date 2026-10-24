using Microsoft.EntityFrameworkCore;
using SWP.Application.DTOs.Medical;
using SWP.Application.Services.Implement;
using SWP.Domain.Common;
using SWP.Domain.Models.Entity;
using SWP.Tests.Support;

namespace SWP.Tests.Services;

public class InjuryWorkflowTests : IDisposable
{
    private readonly TrainingTestContext _t = new();
    private InjuryWorkflowService Service => new(_t.NewContext(), _t.Clock);
    private int Vet => _t.Ids.VetId;
    private int Groom => _t.Ids.GroomId;
    public void Dispose() => _t.Dispose();
    private OpenInjuryCaseRequest Request(int horseId) => new()
    {
        HorseId = horseId, Title = "Theo dõi chấn thương gân", BodyRegion = "Chân trước", Side = "Left",
        ClinicalNotes = "clinical-secret", OwnerSummary = "Đang được theo dõi bởi bác sĩ",
        TrainingGuidance = "trainer-guidance", ReviewDueAt = _t.Clock.GetUtcNow().AddDays(2)
    };
    private async Task<InjuryCaseDto> Open(int? horseId = null) =>
        await Service.CreateCase(Vet, Request(horseId ?? _t.AddHorse("Injury horse")), default);
    private AssignMedicalCareRequest TaskRequest(InjuryCaseDto injury, int? groomId = null) => new()
    {
        Version = injury.Version, GroomId = groomId ?? Groom, Title = "Theo dõi vận động",
        Instructions = "care-instructions-secret", DueAt = _t.Clock.GetUtcNow().AddHours(1)
    };
    private int Exam(int horseId) => _t.Db(db =>
    {
        var exam = new MedicalRecord { HorseId = horseId, CreateBy = Vet, Diagnosis = "original-diagnosis",
            Treatment = "original-treatment", RecordDate = _t.Clock.GetUtcNow().UtcDateTime.Date,
            CreatedAt = _t.Clock.GetUtcNow().UtcDateTime, Status = "Completed" };
        db.MedicalRecords.Add(exam);
        db.SaveChanges();
        return exam.MedicalRecordId;
    });
    private int AnotherUser(int roleSourceId) => _t.Db(db =>
    {
        var user = new User { RoleId = db.Users.Single(u => u.UserId == roleSourceId).RoleId,
            FullName = "Another user", Email = Guid.NewGuid() + "@test.vn", Status = UserStatuses.Active, CreatedAt = TestData.Now.UtcDateTime };
        db.Users.Add(user);
        db.SaveChanges();
        return user.UserId;
    });
    private static async Task Error(int status, Func<Task> action) =>
        Assert.Equal(status, (await Assert.ThrowsAsync<InjuryWorkflowException>(action)).StatusCode);

    [Fact]
    public async Task CaseAndTaskFlow_RequiresResolutionBeforeClosure_AndKeepsHistory()
    {
        var injury = await Open();
        var task = await Service.AssignTask(Vet, injury.Id, TaskRequest(injury), default);
        injury = await Service.GetCase(Vet, injury.Id, default);
        await Error(409, () => Service.ChangeStatus(Vet, injury.Id, new() { Version = injury.Version, Status = "Closed", Reason = "Đã theo dõi xong" }, default));
        await Error(409, () => Service.ExecuteTask(Groom, task.Id, "complete", new() { Version = 1, Notes = "Chưa bắt đầu đã hoàn tất" }, default));
        task = await Service.ExecuteTask(Groom, task.Id, "start", new() { Version = task.Version }, default);
        task = await Service.ExecuteTask(Groom, task.Id, "complete", new() { Version = task.Version, Notes = "Đã theo dõi đủ theo hướng dẫn" }, default);
        await Error(409, () => Service.ExecuteTask(Groom, task.Id, "complete", new() { Version = task.Version, Notes = "Không được ghi lần hai" }, default));
        injury = await Service.GetCase(Vet, injury.Id, default);
        var examId = Exam(injury.HorseId);
        injury = await Service.LinkExam(Vet, injury.Id, new() { Version = injury.Version, MedicalRecordId = examId }, default);
        injury = await Service.ChangeStatus(Vet, injury.Id, new() { Version = injury.Version, Status = "Closed", Reason = "Đã tái khám và hồi phục", ReexamRecordId = examId }, default);
        Assert.Equal("Closed", injury.Status);
        Assert.NotNull(injury.ClosedAt);
        await Error(409, () => Service.AssignTask(Vet, injury.Id, TaskRequest(injury), default));
        var history = await Service.Timeline(Vet, injury.Id, 0, 100, default);
        Assert.Equal(new[] { "CaseOpened", "TaskAssigned", "TaskInProgress", "TaskCompleted", "ExamLinked", "StatusChanged" }, history.Items.Reverse().Select(e => e.Kind));
    }

    [Fact]
    public async Task OwnerSeesOnlyOwnedSummaries_AndNonVetsCannotReadClinicalHistory()
    {
        var injury = await Open();
        var anotherOwner = AnotherUser(_t.Ids.OwnerId);
        var own = await Service.GetCase(_t.Ids.OwnerId, injury.Id, default);
        Assert.Null(own.TrainingGuidance);
        Assert.DoesNotContain("clinical-secret", System.Text.Json.JsonSerializer.Serialize(own));
        Assert.Empty((await Service.ListCases(anotherOwner, null, null, 0, 30, default)).Items);
        await Error(404, () => Service.GetCase(anotherOwner, injury.Id, default));
        foreach (var id in new[] { _t.Ids.OwnerId, _t.Ids.TrainerId, _t.Ids.ManagerId, Groom })
            await Error(403, () => Service.Timeline(id, injury.Id, 0, 30, default));
        Assert.Contains("clinical-secret", (await Service.Timeline(Vet, injury.Id, 0, 30, default)).Items.Single().DetailsJson);
    }

    [Fact]
    public async Task GroomScope_Reassignment_RemovesOldAccess_AndPreservesUnableReason()
    {
        var injury = await Open();
        var task = await Service.AssignTask(Vet, injury.Id, TaskRequest(injury), default);
        var otherGroom = AnotherUser(Groom);
        Assert.Empty((await Service.ListTasks(otherGroom, null, false, 0, 30, default)).Items);
        await Error(404, () => Service.ExecuteTask(otherGroom, task.Id, "start", new() { Version = task.Version }, default));
        task = await Service.ExecuteTask(Groom, task.Id, "unable", new() { Version = task.Version, Notes = "Thiếu dụng cụ, cần bác sĩ xử lý" }, default);
        injury = await Service.GetCase(Vet, injury.Id, default);
        await Error(409, () => Service.ChangeStatus(Vet, injury.Id, new() { Version = injury.Version, Status = "Closed", Reason = "Chưa xử lý không được đóng" }, default));
        task = await Service.ReassignTask(Vet, task.Id, new() { Version = task.Version, GroomId = otherGroom, DueAt = _t.Clock.GetUtcNow().AddHours(3), Notes = "Đã bổ sung dụng cụ và đổi người" }, default);
        Assert.Equal("Pending", task.Status);
        await Error(404, () => Service.ExecuteTask(Groom, task.Id, "start", new() { Version = task.Version }, default));
        Assert.Single((await Service.ListTasks(otherGroom, null, false, 0, 30, default)).Items);
        var unable = (await Service.Timeline(Vet, injury.Id, 0, 30, default)).Items.Single(e => e.Kind == "TaskUnableToComplete");
        using var json = System.Text.Json.JsonDocument.Parse(unable.DetailsJson);
        Assert.Equal("Thiếu dụng cụ, cần bác sĩ xử lý", json.RootElement.GetProperty("notes").GetString());
    }

    [Fact]
    public async Task OverdueIsComputed_ManagersGetOperationalDataWithoutCareInstructions()
    {
        var injury = await Open();
        await Service.AssignTask(Vet, injury.Id, TaskRequest(injury), default);
        _t.Clock.Advance(TimeSpan.FromDays(3));
        var tasks = await Service.ListTasks(_t.Ids.ManagerId, null, true, 0, 30, default);
        Assert.Single(tasks.Items);
        Assert.True(tasks.Items[0].IsOverdue);
        Assert.Equal(71, tasks.Items[0].OverdueHours);
        Assert.Null(tasks.Items[0].Instructions);
        Assert.Null(tasks.Items[0].ResultNotes);
        var overview = await Service.Overview(_t.Ids.ManagerId, default);
        Assert.Equal(1, overview.Overdue);
        Assert.Equal(1, overview.ReviewsOverdue);
        Assert.Equal("Open", (await Service.GetCase(Vet, injury.Id, default)).Status);
    }

    [Fact]
    public async Task OldVersionAndInvalidAssignee_DoNotCreateTasksOrEvents()
    {
        var injury = await Open();
        await Error(400, () => Service.AssignTask(Vet, injury.Id, TaskRequest(injury, _t.Ids.PendingGroomId), default));
        await Error(400, () => Service.AssignTask(Vet, injury.Id, TaskRequest(injury, Vet), default));
        injury = await Service.AddNote(Vet, injury.Id, new() { Version = injury.Version, Note = "Thông tin mới sau lần khám" }, default);
        var request = TaskRequest(injury);
        request.Version = 1;
        await Error(409, () => Service.AssignTask(Vet, injury.Id, request, default));
        Assert.Empty(_t.Db(db => db.CareTasks.ToList()));
        Assert.Equal(2, (await Service.Timeline(Vet, injury.Id, 0, 30, default)).Total);
    }

    [Fact]
    public async Task LinkedExamSnapshotRemains_WhenSourceChanges_AndWrongHorseIsRejected()
    {
        var injury = await Open();
        var examId = Exam(injury.HorseId);
        await Error(400, () => Service.LinkExam(Vet, injury.Id, new() { Version = injury.Version, MedicalRecordId = Exam(_t.AddHorse("Other")) }, default));
        injury = await Service.LinkExam(Vet, injury.Id, new() { Version = injury.Version, MedicalRecordId = examId }, default);
        _t.Db(db => { db.MedicalRecords.Single(r => r.MedicalRecordId == examId).Diagnosis = "changed-diagnosis"; db.SaveChanges(); });
        var snapshot = (await Service.Timeline(Vet, injury.Id, 0, 30, default)).Items.Single(e => e.Kind == "ExamLinked");
        Assert.Contains("original-diagnosis", snapshot.DetailsJson);
        Assert.DoesNotContain("changed-diagnosis", snapshot.DetailsJson);
        await Error(409, () => Service.LinkExam(Vet, injury.Id, new() { Version = injury.Version, MedicalRecordId = examId }, default));
    }

    [Fact]
    public async Task MultipleInjuries_CloseOne_DoesNotReleaseOtherCaseLock()
    {
        var first = await Open();
        var second = await Open(first.HorseId);
        var lock1 = await Service.LockTraining(Vet, first.Id, new() { Version = 1, Reason = "Khóa chấn thương thứ nhất" }, default);
        var lock2 = await Service.LockTraining(Vet, second.Id, new() { Version = 1, Reason = "Khóa chấn thương thứ hai" }, default);
        var examId = Exam(first.HorseId);
        first = await Service.GetCase(Vet, first.Id, default);
        first = await Service.LinkExam(Vet, first.Id, new() { Version = first.Version, MedicalRecordId = examId }, default);
        await Error(409, () => Service.ChangeStatus(Vet, first.Id, new() { Version = first.Version, Status = "Closed", Reason = "Còn khóa nên chưa được đóng", ReexamRecordId = examId }, default));
        await _t.MedicalService().ReleaseTrainingLockAsync(lock1.LockID, new() { NewStatus = "Tốt", ReleaseReason = "Đã tái khám chấn thương thứ nhất", ReexamRecordId = examId }, Vet);
        first = await Service.GetCase(Vet, first.Id, default);
        await Service.ChangeStatus(Vet, first.Id, new() { Version = first.Version, Status = "Closed", Reason = "Đã hồi phục riêng chấn thương này", ReexamRecordId = examId }, default);
        Assert.Null(_t.Db(db => db.TrainingLocks.Single(l => l.LockId == lock2.LockID).NgayGo));
        Assert.False((await _t.FitnessService().GetAssessmentAsync(first.HorseId))!.CanTrain);
        Assert.Contains((await Service.Timeline(Vet, first.Id, 0, 30, default)).Items, e => e.Kind == "TrainingLockReleased");
    }

    [Fact]
    public async Task ReleaseCaseLock_RequiresAnExamLinkedToThatCase()
    {
        var injury = await Open();
        var trainingLock = await Service.LockTraining(Vet, injury.Id, new() { Version = 1, Reason = "Chờ tái khám chấn thương" }, default);
        var examId = Exam(injury.HorseId);
        await Assert.ThrowsAsync<ArgumentException>(() => _t.MedicalService().ReleaseTrainingLockAsync(trainingLock.LockID,
            new() { NewStatus = "Tốt", ReleaseReason = "Chưa gắn bệnh án vào hồ sơ", ReexamRecordId = examId }, Vet));
        Assert.Null(_t.Db(db => db.TrainingLocks.Single(l => l.LockId == trainingLock.LockID).NgayGo));
    }

    [Fact]
    public async Task InactiveAccountCannotMutate_AndOldExamCannotCloseCase()
    {
        var injury = await Open();
        await Error(403, () => Service.CreateCase(_t.Ids.PendingGroomId, Request(injury.HorseId), default));
        var oldExam = Exam(injury.HorseId);
        injury = await Service.LinkExam(Vet, injury.Id, new() { Version = injury.Version, MedicalRecordId = oldExam }, default);
        Exam(injury.HorseId);
        await Error(400, () => Service.ChangeStatus(Vet, injury.Id, new()
        { Version = injury.Version, Status = "Closed", Reason = "Bệnh án này đã cũ hơn lần khám mới", ReexamRecordId = oldExam }, default));
        Assert.Equal("Open", (await Service.GetCase(Vet, injury.Id, default)).Status);
    }

    [Fact]
    public async Task HistoryWriteFailure_RollsBackTaskAndCaseVersion()
    {
        var injury = await Open();
        _t.Db(db => db.Database.ExecuteSqlRaw("""
            CREATE TRIGGER fail_event BEFORE INSERT ON InjuryCaseEvent
            BEGIN SELECT RAISE(ABORT, 'simulated audit storage failure'); END;
            """));
        await Assert.ThrowsAsync<DbUpdateException>(() => Service.AssignTask(Vet, injury.Id, TaskRequest(injury), default));
        Assert.Empty(_t.Db(db => db.CareTasks.ToList()));
        Assert.Equal(1, (await Service.GetCase(Vet, injury.Id, default)).Version);
    }
}
