using SWP.Application.Services.Implement;
using SWP.Application.Training;
using SWP.Domain.Common;
using SWP.Tests.Support;

namespace SWP.Tests.Services;

public class HorseFitnessServiceTests : IDisposable
{
    private readonly TrainingTestContext _t = new();

    public void Dispose() => _t.Dispose();

    [Fact]
    public async Task UnknownHorse_ReturnsNull()
    {
        Assert.Null(await _t.FitnessService().GetAssessmentAsync(9999));
    }

    [Fact]
    public async Task HealthyHorse_IsReady()
    {
        var horseId = _t.AddHorse("Thunder", ageYears: 5);

        var result = await _t.FitnessService().GetAssessmentAsync(horseId);

        Assert.NotNull(result);
        Assert.Equal("Thunder", result.HorseName);
        Assert.Equal(5, result.AgeYears);
        Assert.Equal(480m, result.WeightKg);
        Assert.Equal(HorseHealthStatuses.DuDieuKien, result.HealthStatus);
        Assert.True(result.CanTrain);
        Assert.Equal(FitnessReadiness.Ready, result.Readiness);
        Assert.Equal(FitnessReadiness.Labels[FitnessReadiness.Ready], result.ReadinessLabel);
        Assert.Equal(TrainingIntensityLevels.High, result.MaxAllowedIntensity);
        Assert.Equal(TrainingTypes.All, result.AllowedTrainingTypes);
        Assert.Empty(result.Warnings);
        Assert.Null(result.LatestVitals);
        Assert.Equal(TestData.Now.UtcDateTime, result.AssessedAt);
    }

    [Fact]
    public async Task HorseWithoutHealthRecord_IsConservativelyLimited()
    {
        var horseId = _t.AddHorse("NoRecord", healthStatus: null);

        var result = await _t.FitnessService().GetAssessmentAsync(horseId);

        Assert.Null(result!.HealthStatus);
        Assert.Equal(TrainingIntensityLevels.Medium, result.MaxAllowedIntensity);
        Assert.Equal(FitnessReadiness.Limited, result.Readiness);
    }

    [Fact]
    public async Task LegacyVeterinarianLock_BlocksAllTraining()
    {
        var horseId = _t.AddHorse("Locked", lockReason: "Bong gân cổ chân");

        var result = await _t.FitnessService().GetAssessmentAsync(horseId);

        Assert.True(result!.IsTrainingLocked);
        Assert.Equal("Bong gân cổ chân", result.LockReason);
        Assert.False(result.CanTrain);
        Assert.Equal(FitnessReadiness.NotAllowed, result.Readiness);
        Assert.Empty(result.AllowedTrainingTypes);
    }

    [Fact]
    public async Task QuarantinedHorse_CannotTrain()
    {
        var horseId = _t.AddHorse("Quarantine", healthStatus: HorseHealthStatuses.CachLy);

        var result = await _t.FitnessService().GetAssessmentAsync(horseId);

        Assert.False(result!.CanTrain);
        Assert.Null(result.MaxAllowedIntensity);
        Assert.Empty(result.AllowedTrainingTypes);
    }

    [Fact]
    public async Task RecentFever_BlocksTraining()
    {
        var horseId = _t.AddHorse("Fever");
        _t.Db(db => TestData.AddMetric(db, horseId, TestData.Now.UtcDateTime.AddDays(-1), heartRate: 40, temperature: 39.4m));

        var result = await _t.FitnessService().GetAssessmentAsync(horseId);

        Assert.False(result!.CanTrain);
        Assert.Equal(39.4m, result.LatestVitals!.BodyTemperature);
        Assert.Equal(40m, result.LatestVitals.HeartRate);
    }

    [Fact]
    public async Task VitalsOlderThanWindow_AreIgnored()
    {
        var horseId = _t.AddHorse("OldFever");
        _t.Db(db => TestData.AddMetric(db, horseId, TestData.Now.UtcDateTime.AddDays(-4), temperature: 39.4m));

        var result = await _t.FitnessService().GetAssessmentAsync(horseId);

        Assert.True(result!.CanTrain);
        Assert.Null(result.LatestVitals);
    }

    [Fact]
    public async Task ExerciseReadings_AreNotTreatedAsRestingVitals()
    {
        var horseId = _t.AddHorse("Runner");
        _t.Db(db =>
        {
            TestData.AddMetric(db, horseId, TestData.Now.UtcDateTime.AddHours(-30), heartRate: 38, temperature: 37.6m, speedKmh: 0);
            TestData.AddMetric(db, horseId, TestData.Now.UtcDateTime.AddHours(-1), heartRate: 190, speedKmh: 52);
        });

        var result = await _t.FitnessService().GetAssessmentAsync(horseId);

        Assert.Equal(38m, result!.LatestVitals!.HeartRate);
        Assert.Equal(FitnessReadiness.Ready, result.Readiness);
    }

    [Fact]
    public async Task RecentHighLoad_LowersRecommendation_AndIsSummarised()
    {
        var horseId = _t.AddHorse("Busy");
        var today = TestData.Today;
        _t.Db(db =>
        {
            var (planId, _) = TestData.AddPlan(db, horseId, today.AddDays(-20), today.AddDays(20));
            TestData.AddSession(db, planId, today.AddDays(-1), TrainingTypes.Gallop, TrainingSessionStatuses.Completed, actualDistanceKm: 2.5m);
            TestData.AddSession(db, planId, today.AddDays(-3), TrainingTypes.Interval, TrainingSessionStatuses.Completed, distanceKm: 3m);
            TestData.AddSession(db, planId, today.AddDays(-7), TrainingTypes.TrialRun, TrainingSessionStatuses.Completed, actualDistanceKm: 2m);
            TestData.AddSession(db, planId, today.AddDays(-5), TrainingTypes.Trot, TrainingSessionStatuses.Completed, actualDistanceKm: 4m);
            TestData.AddSession(db, planId, today.AddDays(-2), TrainingTypes.Gallop, TrainingSessionStatuses.Cancelled);
            TestData.AddSession(db, planId, today.AddDays(-10), TrainingTypes.Gallop, TrainingSessionStatuses.Completed);
            TestData.AddSession(db, planId, today.AddDays(1), TrainingTypes.Gallop, TrainingSessionStatuses.Scheduled);
        });

        var result = await _t.FitnessService().GetAssessmentAsync(horseId);

        Assert.Equal(4, result!.RecentLoad.CompletedSessions);
        Assert.Equal(3, result.RecentLoad.HighIntensitySessions);
        Assert.Equal(11.5m, result.RecentLoad.DistanceKm);
        Assert.Equal(TrainingIntensityLevels.High, result.MaxAllowedIntensity);
        Assert.Equal(TrainingIntensityLevels.Medium, result.RecommendedIntensity);
    }

    [Fact]
    public async Task LoadOfAnotherHorse_IsNotCounted()
    {
        var busyId = _t.AddHorse("Busy");
        var restedId = _t.AddHorse("Rested");
        _t.Db(db =>
        {
            var (planId, _) = TestData.AddPlan(db, busyId, TestData.Today.AddDays(-10), TestData.Today.AddDays(10));
            for (var i = 1; i <= 3; i++)
            {
                TestData.AddSession(db, planId, TestData.Today.AddDays(-i), TrainingTypes.Gallop, TrainingSessionStatuses.Completed);
            }
        });

        var result = await _t.FitnessService().GetAssessmentAsync(restedId);

        Assert.Equal(0, result!.RecentLoad.CompletedSessions);
        Assert.Equal(TrainingIntensityLevels.High, result.RecommendedIntensity);
    }

    [Theory]
    [InlineData("2021-10-12", "2026-10-12", 5)]
    [InlineData("2021-10-13", "2026-10-12", 4)]
    [InlineData("2024-02-29", "2026-02-28", 1)]
    [InlineData("2026-10-12", "2026-10-12", 0)]
    public void CalculateAge_CountsCompletedYears(string dob, string today, int expected)
    {
        Assert.Equal(expected, HorseFitnessService.CalculateAge(DateOnly.Parse(dob), DateOnly.Parse(today)));
    }

    [Fact]
    public void CalculateAge_UnknownOrFutureBirthDate_IsNull()
    {
        Assert.Null(HorseFitnessService.CalculateAge(null, TestData.Today));
        Assert.Null(HorseFitnessService.CalculateAge(TestData.Today.AddDays(1), TestData.Today));
    }
}
