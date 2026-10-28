using SWP.Application.Common;
using SWP.Application.DTOs.Races;
using SWP.Domain.Common;
using SWP.Tests.Support;

namespace SWP.Tests.Services;

public class RaceServiceTests : IDisposable
{
    private readonly TrainingTestContext _t = new();

    public void Dispose() => _t.Dispose();

    [Fact]
    public async Task CreateRaceAsync_WithValidData_Succeeds()
    {
        var raceDate = TestData.Now.UtcDateTime.AddDays(10);
        var dto = new CreateRaceDto
        {
            RaceName = "Giải Đua Mùa Xuân Cúp Hoàng Gia",
            RaceCode = "ROYAL-SPRING-01",
            RaceType = "Sprint",
            Location = "Sân vận động Phú Thọ",
            RaceDate = raceDate,
            RegistrationDeadline = raceDate.AddDays(-2),
            DistanceMeters = 1600,
            MaxParticipants = 12,
            EntryFee = 500000,
            PrizePool = 50000000,
            Description = "Giải đua mở rộng đầu xuân"
        };

        var result = await _t.RaceService().CreateRaceAsync(dto, _t.Ids.OwnerId);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.NotNull(result.Data);
        Assert.Equal(dto.RaceName, result.Data.RaceName);
        Assert.Equal("ROYAL-SPRING-01", result.Data.RaceCode);
        Assert.Equal(RaceStatuses.OpenForRegistration, result.Data.Status);
    }

    [Fact]
    public async Task CreateRaceAsync_WithPastDate_ReturnsInvalid()
    {
        var pastDate = TestData.Now.UtcDateTime.AddDays(-1);
        var dto = new CreateRaceDto
        {
            RaceName = "Giải Đua Quá Khứ",
            RaceDate = pastDate
        };

        var result = await _t.RaceService().CreateRaceAsync(dto, _t.Ids.OwnerId);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorCodes.Validation, result.ErrorCode);
    }

    [Fact]
    public async Task GetEligibleHorsesForRaceAsync_FiltersCorrectly()
    {
        var horseHealthy = _t.AddHorse("Chiến Mã Khỏe", healthStatus: HorseHealthStatuses.DuDieuKien);
        var horseInjured = _t.AddHorse("Ngựa Chấn Thương", healthStatus: HorseHealthStatuses.ChanThuong);
        var horseLocked = _t.AddHorse("Ngựa Bị Khóa", healthStatus: HorseHealthStatuses.DuDieuKien, lockReason: "Nghi ngờ nứt móng");

        var raceDate = TestData.Now.UtcDateTime.AddDays(7);
        var raceResult = await _t.RaceService().CreateRaceAsync(new CreateRaceDto
        {
            RaceName = "Giải Thử Nghiệm",
            RaceDate = raceDate
        }, _t.Ids.OwnerId);

        var eligibleList = await _t.RaceService().GetEligibleHorsesForRaceAsync(raceResult.Data!.RaceId);

        Assert.True(eligibleList.Success);
        Assert.NotNull(eligibleList.Data);

        var healthyDto = eligibleList.Data.First(h => h.HorseId == horseHealthy);
        var injuredDto = eligibleList.Data.First(h => h.HorseId == horseInjured);
        var lockedDto = eligibleList.Data.First(h => h.HorseId == horseLocked);

        Assert.True(healthyDto.IsEligible);
        Assert.Null(healthyDto.IneligibilityReason);

        Assert.False(injuredDto.IsEligible);
        Assert.Contains(HorseHealthStatuses.ChanThuong, injuredDto.IneligibilityReason);

        Assert.False(lockedDto.IsEligible);
        Assert.Contains("Nghi ngờ nứt móng", lockedDto.IneligibilityReason);
    }

    [Fact]
    public async Task RegisterHorseForRaceAsync_UnfitHealth_Fails()
    {
        var horseInjured = _t.AddHorse("Ngựa Chấn Thương 2", healthStatus: HorseHealthStatuses.ChanThuong);
        var raceDate = TestData.Now.UtcDateTime.AddDays(5);
        var race = await _t.RaceService().CreateRaceAsync(new CreateRaceDto
        {
            RaceName = "Giải Đua Cúp",
            RaceDate = raceDate
        }, _t.Ids.OwnerId);

        var regResult = await _t.RaceService().RegisterHorseForRaceAsync(
            race.Data!.RaceId,
            new RegisterHorseForRaceDto { HorseId = horseInjured },
            _t.Ids.TrainerId);

        Assert.False(regResult.Success);
        Assert.Equal(ServiceErrorCodes.HorseNotFit, regResult.ErrorCode);
    }

    [Fact]
    public async Task RegisterHorseForRaceAsync_HealthyHorse_RegistersSuccessfully()
    {
        var horseHealthy = _t.AddHorse("Tia Chớp", healthStatus: HorseHealthStatuses.DuDieuKien);
        var raceDate = TestData.Now.UtcDateTime.AddDays(5);
        var race = await _t.RaceService().CreateRaceAsync(new CreateRaceDto
        {
            RaceName = "Giải Đua Đạt Chuẩn",
            RaceDate = raceDate,
            MaxParticipants = 5
        }, _t.Ids.OwnerId);

        var regResult = await _t.RaceService().RegisterHorseForRaceAsync(
            race.Data!.RaceId,
            new RegisterHorseForRaceDto { HorseId = horseHealthy, Notes = "Đăng ký chính thức" },
            _t.Ids.TrainerId);

        Assert.True(regResult.Success, regResult.ErrorMessage);
        Assert.NotNull(regResult.Data);
        Assert.Equal(horseHealthy, regResult.Data.HorseId);
        Assert.Equal(RaceRegistrationStatuses.Confirmed, regResult.Data.RegistrationStatus);
        Assert.Equal(1, regResult.Data.HorseNumber);

        // Đăng ký lại cùng con ngựa phải báo Conflict
        var duplicateReg = await _t.RaceService().RegisterHorseForRaceAsync(
            race.Data.RaceId,
            new RegisterHorseForRaceDto { HorseId = horseHealthy },
            _t.Ids.TrainerId);

        Assert.False(duplicateReg.Success);
        Assert.Equal(ServiceErrorCodes.Conflict, duplicateReg.ErrorCode);
    }

    [Fact]
    public async Task RecordRaceResultsAsync_And_PerformanceReport_CalculatesAccurately()
    {
        var horse = _t.AddHorse("Bạch Long Mã", healthStatus: HorseHealthStatuses.DuDieuKien);
        var raceDate = TestData.Now.UtcDateTime.AddDays(3);
        var race = await _t.RaceService().CreateRaceAsync(new CreateRaceDto
        {
            RaceName = "Giải Vô Địch Quốc Gia",
            RaceDate = raceDate
        }, _t.Ids.OwnerId);

        var reg = await _t.RaceService().RegisterHorseForRaceAsync(
            race.Data!.RaceId,
            new RegisterHorseForRaceDto { HorseId = horse },
            _t.Ids.TrainerId);

        // Nhập kết quả thi đấu: Quán quân hạng 1, thời gian 85.5s, thưởng 20,000,000
        var batchResult = new BatchRecordRaceResultsDto
        {
            Results = new List<RecordRaceResultDto>
            {
                new()
                {
                    RegistrationId = reg.Data!.RegistrationId,
                    FinishPosition = 1,
                    FinishTimeSeconds = 85.5m,
                    PrizeMoney = 20000000,
                    ResultStatus = RaceResultStatuses.Finished,
                    Notes = "Về đích ngoạn mục"
                }
            }
        };

        var recordResult = await _t.RaceService().RecordRaceResultsAsync(race.Data.RaceId, batchResult);
        Assert.True(recordResult.Success, recordResult.ErrorMessage);

        // Giải đấu phải chuyển sang Completed
        var updatedRace = await _t.RaceService().GetRaceByIdAsync(race.Data.RaceId);
        Assert.Equal(RaceStatuses.Completed, updatedRace.Data!.Status);

        // Kiểm tra báo cáo thành tích của ngựa
        var performance = await _t.RaceService().GetHorsePerformanceReportAsync(horse);
        Assert.True(performance.Success);
        Assert.NotNull(performance.Data);
        Assert.Equal(1, performance.Data.TotalRaces);
        Assert.Equal(1, performance.Data.TotalWins);
        Assert.Equal(1, performance.Data.TotalPodiums);
        Assert.Equal(100m, performance.Data.WinRatePercentage);
        Assert.Equal(100m, performance.Data.PodiumRatePercentage);
        Assert.Equal(20000000m, performance.Data.TotalPrizeMoney);
        Assert.Equal(85.5m, performance.Data.BestFinishTimeSeconds);
    }
}
