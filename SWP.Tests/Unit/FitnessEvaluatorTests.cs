using SWP.Application.Training;
using SWP.Domain.Common;

namespace SWP.Tests.Unit;

public class FitnessEvaluatorTests
{
    private static FitnessInput Healthy(int age = 5) => new()
    {
        AgeYears = age,
        HealthStatus = HorseHealthStatuses.DuDieuKien,
    };

    [Fact]
    public void HealthyAdultHorse_IsReadyForHighIntensity()
    {
        var result = FitnessEvaluator.Evaluate(Healthy());

        Assert.True(result.CanTrain);
        Assert.Equal(FitnessReadiness.Ready, result.Readiness);
        Assert.Equal(TrainingIntensityLevels.High, result.MaxAllowedIntensity);
        Assert.Equal(TrainingIntensityLevels.High, result.RecommendedIntensity);
        Assert.Empty(result.Warnings);
    }

    [Theory]
    [InlineData(HorseHealthStatuses.CachLy, null, FitnessReadiness.NotAllowed)]
    [InlineData(HorseHealthStatuses.ChanThuong, TrainingIntensityLevels.Recovery, FitnessReadiness.RecoveryOnly)]
    [InlineData(HorseHealthStatuses.CanTheoDoi, TrainingIntensityLevels.Low, FitnessReadiness.Limited)]
    [InlineData(null, TrainingIntensityLevels.Medium, FitnessReadiness.Limited)]
    [InlineData("Trạng thái lạ", TrainingIntensityLevels.Medium, FitnessReadiness.Limited)]
    public void HealthStatus_CapsIntensity(string? status, string? expectedMax, string expectedReadiness)
    {
        var result = FitnessEvaluator.Evaluate(Healthy() with { HealthStatus = status });

        Assert.Equal(expectedMax, result.MaxAllowedIntensity);
        Assert.Equal(expectedReadiness, result.Readiness);
        Assert.Equal(expectedMax is not null, result.CanTrain);
        Assert.Single(result.Warnings);
    }

    [Fact]
    public void Quarantine_CannotTrain_AndHasNoRecommendation()
    {
        var result = FitnessEvaluator.Evaluate(Healthy() with { HealthStatus = HorseHealthStatuses.CachLy });

        Assert.False(result.CanTrain);
        Assert.Null(result.MaxAllowedIntensity);
        Assert.Null(result.RecommendedIntensity);
        Assert.Contains("cách ly", result.Warnings[0]);
    }

    [Fact]
    public void VeterinarianLock_AllowsRecoveryOnly_AndShowsReason()
    {
        var result = FitnessEvaluator.Evaluate(Healthy() with
        {
            IsTrainingLocked = true,
            LockReason = "Viêm gân chân trước",
        });

        Assert.True(result.CanTrain);
        Assert.Equal(TrainingIntensityLevels.Recovery, result.MaxAllowedIntensity);
        Assert.Equal(FitnessReadiness.RecoveryOnly, result.Readiness);
        Assert.Contains("Viêm gân chân trước", result.Warnings.Single());
    }

    [Theory]
    [InlineData(39.0, null)]
    [InlineData(39.6, null)]
    [InlineData(38.7, TrainingIntensityLevels.Recovery)]
    [InlineData(38.5, TrainingIntensityLevels.High)]
    [InlineData(37.8, TrainingIntensityLevels.High)]
    public void BodyTemperature_Thresholds(double temperature, string? expectedMax)
    {
        var result = FitnessEvaluator.Evaluate(Healthy() with { BodyTemperature = (decimal)temperature });

        Assert.Equal(expectedMax, result.MaxAllowedIntensity);
    }

    [Theory]
    [InlineData(36, TrainingIntensityLevels.High)]
    [InlineData(44, TrainingIntensityLevels.High)]
    [InlineData(50, TrainingIntensityLevels.Low)]
    [InlineData(60, TrainingIntensityLevels.Low)]
    [InlineData(65, TrainingIntensityLevels.Recovery)]
    public void RestingHeartRate_Thresholds(int heartRate, string expectedMax)
    {
        var result = FitnessEvaluator.Evaluate(Healthy() with { RestingHeartRate = heartRate });

        Assert.Equal(expectedMax, result.MaxAllowedIntensity);
    }

    [Theory]
    [InlineData(16, TrainingIntensityLevels.High)]
    [InlineData(20, TrainingIntensityLevels.High)]
    [InlineData(28, TrainingIntensityLevels.Low)]
    public void RespiratoryRate_Thresholds(int respiratoryRate, string expectedMax)
    {
        var result = FitnessEvaluator.Evaluate(Healthy() with { RespiratoryRate = respiratoryRate });

        Assert.Equal(expectedMax, result.MaxAllowedIntensity);
    }

    [Theory]
    [InlineData(0, TrainingIntensityLevels.Low)]
    [InlineData(1, TrainingIntensityLevels.Low)]
    [InlineData(2, TrainingIntensityLevels.High)]
    [InlineData(17, TrainingIntensityLevels.High)]
    [InlineData(18, TrainingIntensityLevels.Medium)]
    [InlineData(24, TrainingIntensityLevels.Medium)]
    public void Age_Thresholds(int age, string expectedMax)
    {
        var result = FitnessEvaluator.Evaluate(Healthy(age));

        Assert.Equal(expectedMax, result.MaxAllowedIntensity);
    }

    [Fact]
    public void UnknownAge_IsNotRestricted()
    {
        var result = FitnessEvaluator.Evaluate(Healthy() with { AgeYears = null });

        Assert.Equal(TrainingIntensityLevels.High, result.MaxAllowedIntensity);
    }

    [Fact]
    public void MultipleFactors_TakeTheStrictestCap_AndListEveryWarning()
    {
        var result = FitnessEvaluator.Evaluate(new FitnessInput
        {
            AgeYears = 20,
            HealthStatus = HorseHealthStatuses.CanTheoDoi,
            RestingHeartRate = 65,
        });

        Assert.Equal(TrainingIntensityLevels.Recovery, result.MaxAllowedIntensity);
        Assert.Equal(3, result.Warnings.Count);
    }

    [Fact]
    public void HeavyRecentLoad_LowersRecommendation_ButNotTheHardCap()
    {
        var result = FitnessEvaluator.Evaluate(Healthy() with { HighIntensitySessionsLast7Days = 3 });

        Assert.Equal(TrainingIntensityLevels.High, result.MaxAllowedIntensity);
        Assert.Equal(TrainingIntensityLevels.Medium, result.RecommendedIntensity);
        Assert.Equal(FitnessReadiness.Ready, result.Readiness);
        Assert.Contains("giảm tải", result.Warnings.Single());
    }

    [Fact]
    public void ModerateRecentLoad_DoesNotChangeRecommendation()
    {
        var result = FitnessEvaluator.Evaluate(Healthy() with { HighIntensitySessionsLast7Days = 2 });

        Assert.Equal(TrainingIntensityLevels.High, result.RecommendedIntensity);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void HeavyLoad_WhenAlreadyCappedBelowHigh_AddsNoExtraWarning()
    {
        var result = FitnessEvaluator.Evaluate(Healthy() with
        {
            HealthStatus = HorseHealthStatuses.CanTheoDoi,
            HighIntensitySessionsLast7Days = 4,
        });

        Assert.Equal(TrainingIntensityLevels.Low, result.RecommendedIntensity);
        Assert.Single(result.Warnings);
    }
}
