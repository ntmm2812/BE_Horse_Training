using SWP.Application.Common;
using SWP.Application.DTOs.Veterinary;
using SWP.Application.Training;
using SWP.Domain.Common;
using SWP.Tests.Support;

namespace SWP.Tests.Services;

public class VeterinaryServiceTests : IDisposable
{
    private readonly TrainingTestContext _t = new();

    public void Dispose() => _t.Dispose();

    [Fact]
    public async Task Exam_WithInjuryAndChanThuong_LocksHorse_AndLimitsTraining()
    {
        var horseId = _t.AddHorse("Star");

        var saved = await _t.VetService().CreateRecordAsync(Exam(horseId, HorseHealthStatuses.ChanThuong, "Gân"), _t.Ids.VetId);

        Assert.True(saved.Success, saved.ErrorMessage);
        Assert.True(saved.Data!.HealthUpdated);
        Assert.NotNull(saved.Data.Lock);
        Assert.Contains("Gân", saved.Data.Lock!.LyDo);

        var horse = _t.Db(db => db.Horses.Single(h => h.HorseId == horseId));
        Assert.NotNull(horse.LockedAt);
        Assert.Equal(_t.Ids.VetId, horse.LockedByUserId);

        var fitness = await _t.FitnessService().GetAssessmentAsync(horseId);
        Assert.Equal(FitnessReadiness.RecoveryOnly, fitness!.Readiness);
        Assert.Equal(HorseHealthStatuses.ChanThuong, fitness.HealthStatus);
    }

    [Fact]
    public async Task Exam_CanTheoDoi_DoesNotLock()
    {
        var horseId = _t.AddHorse("Watch");

        var saved = await _t.VetService().CreateRecordAsync(Exam(horseId, HorseHealthStatuses.CanTheoDoi, "Gân"), _t.Ids.VetId);

        Assert.True(saved.Success, saved.ErrorMessage);
        Assert.Null(saved.Data!.Lock);
        var horse = _t.Db(db => db.Horses.Single(h => h.HorseId == horseId));
        Assert.Null(horse.LockedAt);
    }

    [Fact]
    public async Task OlderExam_DoesNotOverwriteNewerHealth()
    {
        var horseId = _t.AddHorse("Fresh");
        var request = Exam(horseId, HorseHealthStatuses.CachLy, InjuryCatalog.None);
        request.Ngay = TestData.Today.AddDays(-3).ToString("yyyy-MM-dd");

        var saved = await _t.VetService().CreateRecordAsync(request, _t.Ids.VetId);

        Assert.True(saved.Success, saved.ErrorMessage);
        Assert.False(saved.Data!.HealthUpdated);
        var status = _t.Db(db => db.HorseHealthStatuses.Single(s => s.HorseId == horseId).TrangThai);
        Assert.Equal(HorseHealthStatuses.DuDieuKien, status);
    }

    [Fact]
    public async Task SecondLock_IsRejected()
    {
        var horseId = _t.AddHorse("Locked");
        var first = await _t.VetService().CreateLockAsync(Lock(horseId), _t.Ids.VetId);
        Assert.True(first.Success, first.ErrorMessage);

        var second = await _t.VetService().CreateLockAsync(Lock(horseId), _t.Ids.VetId);

        Assert.False(second.Success);
        Assert.Equal(ServiceErrorCodes.Conflict, second.ErrorCode);
    }

    [Fact]
    public async Task ReleaseLock_ClearsHorse_AndSetsFollowUpStatus()
    {
        var horseId = _t.AddHorse("Rehab");
        var created = await _t.VetService().CreateLockAsync(Lock(horseId), _t.Ids.VetId);
        Assert.True(created.Success, created.ErrorMessage);

        var released = await _t.VetService().ReleaseLockAsync(created.Data!.LockID, HorseHealthStatuses.CanTheoDoi, null);

        Assert.True(released.Success, released.ErrorMessage);
        Assert.Equal(TestData.Today.ToString("yyyy-MM-dd"), released.Data!.NgayGo);
        var horse = _t.Db(db => db.Horses.Single(h => h.HorseId == horseId));
        Assert.Null(horse.LockedAt);
        Assert.Null(horse.LockReason);
        var status = _t.Db(db => db.HorseHealthStatuses.Single(s => s.HorseId == horseId).TrangThai);
        Assert.Equal(HorseHealthStatuses.CanTheoDoi, status);

        var fitness = await _t.FitnessService().GetAssessmentAsync(horseId);
        Assert.Equal(FitnessReadiness.Limited, fitness!.Readiness);
    }

    [Fact]
    public async Task OtherVet_CannotEditExam()
    {
        var horseId = _t.AddHorse("Chart");
        var created = await _t.VetService().CreateRecordAsync(Exam(horseId, HorseHealthStatuses.DuDieuKien, InjuryCatalog.None), _t.Ids.VetId);
        Assert.True(created.Success, created.ErrorMessage);

        var edited = await _t.VetService().UpdateRecordAsync(
            created.Data!.Record.RecordID,
            Exam(horseId, HorseHealthStatuses.DuDieuKien, InjuryCatalog.None),
            _t.Ids.TrainerId);

        Assert.False(edited.Success);
        Assert.Equal(ServiceErrorCodes.Conflict, edited.ErrorCode);
    }

    [Fact]
    public async Task DuplicatePendingVaccine_IsRejected_AndCompleteCanScheduleNext()
    {
        var horseId = _t.AddHorse("Needle");
        var first = await _t.VetService().CreateVaccinationAsync(Vaccine(horseId, TestData.Today));
        Assert.True(first.Success, first.ErrorMessage);

        var duplicate = await _t.VetService().CreateVaccinationAsync(Vaccine(horseId, TestData.Today.AddDays(10)));
        Assert.False(duplicate.Success);
        Assert.Equal(ServiceErrorCodes.Conflict, duplicate.ErrorCode);

        var done = await _t.VetService().MarkVaccinatedAsync(first.Data!.ScheduleID, TestData.Today.AddDays(30).ToString("yyyy-MM-dd"));
        Assert.True(done.Success, done.ErrorMessage);
        Assert.Equal(VaccineStatuses.Done, done.Data!.Item.TrangThai);
        Assert.NotNull(done.Data.Next);
        Assert.Equal(VaccineStatuses.Pending, done.Data.Next!.TrangThai);
    }

    [Fact]
    public async Task FutureExamDate_IsRejected()
    {
        var horseId = _t.AddHorse("Tomorrow");
        var request = Exam(horseId, HorseHealthStatuses.DuDieuKien, InjuryCatalog.None);
        request.Ngay = TestData.Today.AddDays(1).ToString("yyyy-MM-dd");

        var saved = await _t.VetService().CreateRecordAsync(request, _t.Ids.VetId);

        Assert.False(saved.Success);
        Assert.Equal("Ngày khám không được ở tương lai", saved.ErrorMessage);
    }

    private static SaveMedicalRecordRequestDto Exam(int horseId, string status, string injury) => new()
    {
        HorseID = horseId,
        Ngay = TestData.Today.ToString("yyyy-MM-dd"),
        ChanDoan = "Viêm gân nhẹ sau buổi chạy",
        PhacDo = "Nghỉ gallop, chườm lạnh và tái khám sau 7 ngày",
        ViTriChanThuong = injury,
        TrangThai = status,
    };

    private static CreateTrainingLockRequestDto Lock(int horseId) => new()
    {
        HorseID = horseId,
        LyDo = "Viêm gân chân trước",
        NgayDat = TestData.Today.ToString("yyyy-MM-dd"),
    };

    private static SaveVaccinationRequestDto Vaccine(int horseId, DateOnly date) => new()
    {
        HorseID = horseId,
        Loai = "cúm ngựa",
        NgayHen = date.ToString("yyyy-MM-dd"),
    };
}
