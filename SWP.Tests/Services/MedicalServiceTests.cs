using SWP.Application.DTOs.Medical;
using SWP.Domain.Models.Entity;
using SWP.Tests.Support;

namespace SWP.Tests.Services;

public class MedicalServiceTests : IDisposable
{
    private readonly TrainingTestContext _t = new();

    public void Dispose() => _t.Dispose();

    [Fact]
    public async Task CreateMedicalRecord_WithInjuryAndTreatmentState_AutoLocksHorse()
    {
        var horseId = _t.AddHorse("Storm", ageYears: 4, healthStatus: "Đủ điều kiện");
        var svc = _t.MedicalService();

        var request = new SaveMedicalRecordRequestDto
        {
            HorseID = horseId,
            Ngay = DateTime.Today.ToString("yyyy-MM-dd"),
            ChanDoan = "Viêm gân gấp chân trước trái",
            PhacDo = "Nghỉ tập hoàn toàn 3 tuần, chườm lạnh 20 phút mỗi ngày.",
            Injuries = ["Chân trước trái", "Gân"],
            TrangThai = "Đang điều trị"
        };

        var result = await svc.CreateMedicalRecordAsync(request, _t.Ids.VetId);

        Assert.NotNull(result.Record);
        Assert.Equal(horseId, result.Record.HorseID);
        Assert.Equal("Viêm gân gấp chân trước trái", result.Record.ChanDoan);
        Assert.Contains("Chân trước trái", result.Record.ViTriChanThuong);
        Assert.True(result.HealthUpdated);
        Assert.NotNull(result.Lock);
        Assert.Equal(horseId, result.Lock!.HorseID);
        Assert.Null(result.Lock.NgayGo);

        // Verify active locks query
        var activeLocks = await svc.GetActiveTrainingLocksAsync();
        Assert.Contains(activeLocks, l => l.HorseID == horseId);
    }

    [Fact]
    public async Task CreateMedicalRecord_WithoutInjury_DoesNotLockHorse()
    {
        var horseId = _t.AddHorse("Thunder", ageYears: 5, healthStatus: "Đủ điều kiện");
        var svc = _t.MedicalService();

        var request = new SaveMedicalRecordRequestDto
        {
            HorseID = horseId,
            Ngay = DateTime.Today.ToString("yyyy-MM-dd"),
            ChanDoan = "Khám định kỳ, tim phổi và vận động tốt",
            PhacDo = "Duy trì chế độ ăn và tập luyện hiện tại bình thường.",
            Injuries = ["Không có chấn thương"],
            TrangThai = "Tốt"
        };

        var result = await svc.CreateMedicalRecordAsync(request, _t.Ids.VetId);

        Assert.NotNull(result.Record);
        Assert.Null(result.Lock);

        var activeLocks = await svc.GetActiveTrainingLocksAsync();
        Assert.DoesNotContain(activeLocks, l => l.HorseID == horseId);
    }

    [Fact]
    public async Task CreateMedicalRecord_InvalidDiagnosis_ThrowsException()
    {
        var horseId = _t.AddHorse("Flash", ageYears: 5);
        var svc = _t.MedicalService();

        var request = new SaveMedicalRecordRequestDto
        {
            HorseID = horseId,
            Ngay = DateTime.Today.ToString("yyyy-MM-dd"),
            ChanDoan = "Đau", // < 5 chars
            PhacDo = "Điều trị chi tiết hơn mười ký tự.",
            Injuries = ["Không có chấn thương"]
        };

        await Assert.ThrowsAsync<ArgumentException>(() => svc.CreateMedicalRecordAsync(request, _t.Ids.VetId));
    }

    [Fact]
    public async Task CreateTrainingLock_Valid_UpdatesHealthStatus()
    {
        var horseId = _t.AddHorse("Blaze", ageYears: 6, healthStatus: "Đủ điều kiện");
        var svc = _t.MedicalService();

        var lockDto = await svc.CreateTrainingLockAsync(new CreateTrainingLockRequestDto
        {
            HorseID = horseId,
            LyDo = "Viêm gân nhẹ chân trước",
            NgayDat = DateTime.Today.ToString("yyyy-MM-dd")
        }, _t.Ids.VetId);

        Assert.NotNull(lockDto);
        Assert.Equal(horseId, lockDto.HorseID);
        Assert.Null(lockDto.NgayGo);

        // Check health status changed to "Chấn thương"
        var health = _t.Db(db => db.HorseHealthStatuses.First(h => h.HorseId == horseId));
        Assert.Equal("Chấn thương", health.TrangThai);
    }

    [Fact]
    public async Task ReleaseTrainingLock_Valid_UpdatesHealthStatusAndSetsNgayGo()
    {
        var horseId = _t.AddHorse("Spirit", ageYears: 4, healthStatus: "Đang điều trị");
        var svc = _t.MedicalService();

        var lockDto = await svc.CreateTrainingLockAsync(new CreateTrainingLockRequestDto
        {
            HorseID = horseId,
            LyDo = "Sưng nhẹ khớp cổ chân",
            NgayDat = DateTime.Today.ToString("yyyy-MM-dd")
        }, _t.Ids.VetId);

        var exam = await svc.CreateMedicalRecordAsync(new SaveMedicalRecordRequestDto
        {
            HorseID = horseId,
            Ngay = DateTime.Today.ToString("yyyy-MM-dd"),
            ChanDoan = "Tái khám, vận động đã hồi phục",
            PhacDo = "Theo dõi vận động sau khi mở khóa",
            Injuries = ["Không có chấn thương"]
        }, _t.Ids.VetId);

        var released = await svc.ReleaseTrainingLockAsync(lockDto.LockID, new ReleaseTrainingLockRequestDto
        {
            NewStatus = "Tốt",
            ReleaseReason = "Đã tái khám và đủ điều kiện tập",
            ReexamRecordId = exam.Record.RecordID
        }, _t.Ids.VetId);

        Assert.NotNull(released.NgayGo);
        Assert.Equal(DateTime.Today.ToString("yyyy-MM-dd"), released.NgayGo);

        var health = _t.Db(db => db.HorseHealthStatuses.First(h => h.HorseId == horseId));
        Assert.Equal("Đủ điều kiện", health.TrangThai);

        var audit = _t.Db(db => db.AuditLogs.First(a => a.Action == "ReleaseTrainingLock" && a.RecordId == lockDto.LockID));
        Assert.Equal(_t.Ids.VetId, audit.UserId);
        Assert.False((await _t.FitnessService().GetAssessmentAsync(horseId))!.IsTrainingLocked);
    }

    [Fact]
    public async Task VaccinationCRUD_And_MarkDone_CreatesNextItem()
    {
        var horseId = _t.AddHorse("Shadow", ageYears: 3);
        var svc = _t.MedicalService();

        var created = await svc.CreateVaccinationAsync(new SaveVaccinationRequestDto
        {
            HorseID = horseId,
            Loai = "Cúm ngựa",
            NgayHen = DateTime.Today.AddDays(7).ToString("yyyy-MM-dd")
        });

        Assert.NotNull(created);
        Assert.Equal("Chờ tiêm", created.TrangThai);
        Assert.Equal("Cúm ngựa", created.Loai);

        // Duplicate pending throws
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateVaccinationAsync(new SaveVaccinationRequestDto
        {
            HorseID = horseId,
            Loai = "Cúm ngựa",
            NgayHen = DateTime.Today.AddDays(14).ToString("yyyy-MM-dd")
        }));

        // Mark done with next date
        var nextDate = DateTime.Today.AddDays(180).ToString("yyyy-MM-dd");
        var doneResult = await svc.MarkVaccinatedAsync(created.ScheduleID, nextDate);

        Assert.Equal("Đã tiêm", doneResult.Item.TrangThai);
        Assert.NotNull(doneResult.Next);
        Assert.Equal("Chờ tiêm", doneResult.Next!.TrangThai);
        Assert.Equal(nextDate, doneResult.Next.NgayHen);
    }
}
