using SWP.Application.DTOs.Medical;
using SWP.Application.Common;
using SWP.Domain.Common;
using SWP.Domain.Models.Entity;
using SWP.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace SWP.Tests.Services;

public class MedicalTrainingFlowTests : IDisposable
{
    private readonly TrainingTestContext _t = new();
    public void Dispose() => _t.Dispose();

    private SaveMedicalRecordRequestDto Exam(int horseId, string? status = null) => new()
    {
        HorseID = horseId, Ngay = DateTime.Today.ToString("yyyy-MM-dd"),
        ChanDoan = "Đã khám và kiểm tra vận động", PhacDo = "Theo dõi vận động sau tái khám",
        Injuries = ["Không có chấn thương"], TrangThai = status
    };

    private Task<TrainingLockDto> Lock(int horseId) => _t.MedicalService().CreateTrainingLockAsync(new()
    {
        HorseID = horseId, LyDo = "Dừng tập để kiểm tra chấn thương", NgayDat = DateTime.Today.ToString("yyyy-MM-dd")
    }, _t.Ids.VetId);

    [Fact]
    public async Task MedicalLock_BlocksExistingAndNewRecoverySessions_UntilReviewedRelease()
    {
        var horseId = _t.AddHorse("Medical training flow");
        var planId = await _t.CreateActivePlanAsync(horseId);
        var scheduled = await _t.SessionService().CreateAsync(planId, TestData.Session(TestData.Today, TrainingTypes.Walk));
        Assert.True(scheduled.Success, scheduled.ErrorMessage);
        var locked = await Lock(horseId);
        Assert.Null(_t.Db(db => db.Horses.Single(h => h.HorseId == horseId).LockedAt));
        Assert.False((await _t.FitnessService().GetAssessmentAsync(horseId))!.CanTrain);
        var start = await _t.SessionService().StartAsync(scheduled.Data!.SessionId);
        var create = await _t.SessionService().CreateAsync(planId, TestData.Session(TestData.Today.AddDays(1), TrainingTypes.Walk));
        Assert.Equal(ServiceErrorCodes.HorseNotFit, start.ErrorCode);
        Assert.Equal(ServiceErrorCodes.HorseNotFit, create.ErrorCode);

        var exam = await _t.MedicalService().CreateMedicalRecordAsync(Exam(horseId), _t.Ids.VetId);
        await _t.MedicalService().ReleaseTrainingLockAsync(locked.LockID, new()
        {
            NewStatus = "Tốt", ReleaseReason = "Tái khám đủ điều kiện vận động", ReexamRecordId = exam.Record.RecordID
        }, _t.Ids.VetId);
        Assert.True((await _t.FitnessService().GetAssessmentAsync(horseId))!.CanTrain);
        Assert.Equal(TrainingSessionStatuses.Scheduled, _t.Db(db => db.TrainingSchedules.Single(s => s.ScheduleId == scheduled.Data.SessionId).Status));
        Assert.True((await _t.SessionService().StartAsync(scheduled.Data.SessionId)).Success);
    }

    [Fact]
    public async Task FailedAutoLock_RollsBackExamAndHealthTogether()
    {
        var horseId = _t.AddHorse("Rollback");
        _t.Db(db => db.Database.ExecuteSqlRaw("""
            CREATE TRIGGER fail_lock BEFORE INSERT ON TrainingLock
            BEGIN SELECT RAISE(ABORT, 'simulated lock storage failure'); END;
            """));
        var request = Exam(horseId, "Đang điều trị");
        request.Injuries = ["Chân trước trái"];
        await Assert.ThrowsAsync<DbUpdateException>(() => _t.MedicalService().CreateMedicalRecordAsync(request, _t.Ids.VetId));
        Assert.Empty(_t.Db(db => db.MedicalRecords.ToList()));
        Assert.Empty(_t.Db(db => db.TrainingLocks.ToList()));
        Assert.Equal(HorseHealthStatuses.DuDieuKien, _t.Db(db => db.HorseHealthStatuses.Single(h => h.HorseId == horseId).TrangThai));
    }

    [Fact]
    public async Task InvalidStatus_DoesNotPartiallySaveExam()
    {
        var horseId = _t.AddHorse("Invalid status");
        await Assert.ThrowsAsync<ArgumentException>(() => _t.MedicalService().CreateMedicalRecordAsync(Exam(horseId, "invalid"), _t.Ids.VetId));
        Assert.Empty(_t.Db(db => db.MedicalRecords.ToList()));
        Assert.Equal(HorseHealthStatuses.DuDieuKien, _t.Db(db => db.HorseHealthStatuses.Single(h => h.HorseId == horseId).TrangThai));
    }

    [Fact]
    public async Task ReleaseWithAnotherHorsesExam_DoesNotUnlock()
    {
        var horseId = _t.AddHorse("Locked horse");
        var otherId = _t.AddHorse("Other horse");
        var locked = await Lock(horseId);
        var exam = await _t.MedicalService().CreateMedicalRecordAsync(Exam(otherId), _t.Ids.VetId);
        await Assert.ThrowsAsync<ArgumentException>(() => _t.MedicalService().ReleaseTrainingLockAsync(locked.LockID, new()
        {
            NewStatus = "Tốt", ReleaseReason = "Đã kiểm tra lại vận động", ReexamRecordId = exam.Record.RecordID
        }, _t.Ids.VetId));
        Assert.Null(_t.Db(db => db.TrainingLocks.Single(l => l.LockId == locked.LockID).NgayGo));
        Assert.False((await _t.FitnessService().GetAssessmentAsync(horseId))!.CanTrain);
    }

    [Fact]
    public async Task ReleaseOneLock_PreservesAnotherActiveLockAndHealth()
    {
        var horseId = _t.AddHorse("Multiple old locks");
        var locked = await Lock(horseId);
        _t.Db(db =>
        {
            db.TrainingLocks.Add(new TrainingLock { HorseId = horseId, VetId = _t.Ids.VetId,
                LyDo = "Khóa cũ khác vẫn hiệu lực", NgayDat = DateOnly.FromDateTime(DateTime.Today) });
            db.SaveChanges();
        });
        var exam = await _t.MedicalService().CreateMedicalRecordAsync(Exam(horseId), _t.Ids.VetId);
        await _t.MedicalService().ReleaseTrainingLockAsync(locked.LockID, new()
        {
            NewStatus = "Tốt", ReleaseReason = "Tái khám riêng một chấn thương", ReexamRecordId = exam.Record.RecordID
        }, _t.Ids.VetId);
        Assert.False((await _t.FitnessService().GetAssessmentAsync(horseId))!.CanTrain);
        Assert.Equal(HorseHealthStatuses.ChanThuong, _t.Db(db => db.HorseHealthStatuses.Single(h => h.HorseId == horseId).TrangThai));
    }

    [Fact]
    public async Task LockAndRelease_DoNotClearQuarantine()
    {
        var horseId = _t.AddHorse("Quarantined horse", healthStatus: HorseHealthStatuses.CachLy);
        var locked = await Lock(horseId);
        var exam = await _t.MedicalService().CreateMedicalRecordAsync(Exam(horseId), _t.Ids.VetId);
        await _t.MedicalService().ReleaseTrainingLockAsync(locked.LockID, new()
        {
            NewStatus = "Tốt", ReleaseReason = "Chấn thương hết nhưng vẫn cách ly", ReexamRecordId = exam.Record.RecordID
        }, _t.Ids.VetId);
        Assert.Equal(HorseHealthStatuses.CachLy, _t.Db(db => db.HorseHealthStatuses.Single(h => h.HorseId == horseId).TrangThai));
        Assert.False((await _t.FitnessService().GetAssessmentAsync(horseId))!.CanTrain);
    }
}
