using SWP.Application.Common;
using SWP.Application.Training;
using SWP.Domain.Common;

namespace SWP.Tests.Unit;

public class TrainingCatalogTests
{
    [Fact]
    public void IntensityLevels_AreOrderedFromLightToHeavy()
    {
        Assert.Equal(
            [1, 2, 3, 4],
            TrainingIntensityLevels.All.Select(TrainingIntensityLevels.Rank).ToArray());
        Assert.Equal(0, TrainingIntensityLevels.Rank("Extreme"));
        Assert.Equal(0, TrainingIntensityLevels.Rank(null));
        Assert.Equal(4, TrainingIntensityLevels.Rank(" high "));
        Assert.Null(TrainingIntensityLevels.FromRank(0));
        Assert.Null(TrainingIntensityLevels.FromRank(5));
    }

    [Theory]
    [InlineData("Low", "High", true)]
    [InlineData("High", "High", true)]
    [InlineData("High", "Medium", false)]
    [InlineData("Low", null, false)]
    [InlineData(null, "High", false)]
    public void IsWithin_ComparesRanks(string? level, string? limit, bool expected)
    {
        Assert.Equal(expected, TrainingIntensityLevels.IsWithin(level, limit));
    }

    [Fact]
    public void Min_ReturnsLighterLevel()
    {
        Assert.Equal(TrainingIntensityLevels.Low, TrainingIntensityLevels.Min(TrainingIntensityLevels.High, TrainingIntensityLevels.Low));
        Assert.Equal(TrainingIntensityLevels.Recovery, TrainingIntensityLevels.Min(TrainingIntensityLevels.Recovery, TrainingIntensityLevels.Medium));
    }

    [Theory]
    [InlineData(TrainingTypes.Walk, TrainingIntensityLevels.Recovery, TrackSurfaces.Sand)]
    [InlineData(TrainingTypes.Swimming, TrainingIntensityLevels.Recovery, TrackSurfaces.Pool)]
    [InlineData(TrainingTypes.Trot, TrainingIntensityLevels.Low, TrackSurfaces.Sand)]
    [InlineData(TrainingTypes.Canter, TrainingIntensityLevels.Medium, TrackSurfaces.Dirt)]
    [InlineData(TrainingTypes.Interval, TrainingIntensityLevels.High, TrackSurfaces.Dirt)]
    [InlineData(TrainingTypes.Gallop, TrainingIntensityLevels.High, TrackSurfaces.Turf)]
    [InlineData(TrainingTypes.TrialRun, TrainingIntensityLevels.High, TrackSurfaces.Turf)]
    public void TrainingTypes_HaveIntensityAndDefaultSurface(string type, string intensity, string surface)
    {
        Assert.Equal(intensity, TrainingTypes.GetIntensity(type));
        Assert.Equal(surface, TrainingTypes.GetDefaultSurface(type));
        Assert.True(TrainingTypes.Labels.ContainsKey(type));
    }

    [Fact]
    public void TrainingTypes_LookupIsCaseInsensitive_AndUnknownHasNoIntensity()
    {
        Assert.True(TrainingTypes.TryNormalize("gallop", out var normalized));
        Assert.Equal(TrainingTypes.Gallop, normalized);
        Assert.Null(TrainingTypes.GetIntensity("Cardio"));
        Assert.Null(TrainingTypes.GetIntensity(null));
    }

    [Theory]
    [InlineData(TrainingTypes.Swimming, TrackSurfaces.Pool, true)]
    [InlineData(TrainingTypes.Swimming, TrackSurfaces.Sand, false)]
    [InlineData(TrainingTypes.Gallop, TrackSurfaces.Pool, false)]
    [InlineData(TrainingTypes.Gallop, TrackSurfaces.Turf, true)]
    public void Surfaces_PoolIsOnlyForSwimming(string type, string surface, bool expected)
    {
        Assert.Equal(expected, TrackSurfaces.IsCompatible(type, surface));
    }

    [Fact]
    public void EveryCode_HasAVietnameseLabel()
    {
        Assert.All(TrainingIntensityLevels.All, c => Assert.True(TrainingIntensityLevels.Labels.ContainsKey(c)));
        Assert.All(TrackSurfaces.All, c => Assert.True(TrackSurfaces.Labels.ContainsKey(c)));
        Assert.All(TrainingPlanStatuses.All, c => Assert.True(TrainingPlanStatuses.Labels.ContainsKey(c)));
        Assert.All(TrainingSessionStatuses.All, c => Assert.True(TrainingSessionStatuses.Labels.ContainsKey(c)));
        Assert.All(TrainingPlanTypes.All, c => Assert.True(TrainingPlanTypes.Labels.ContainsKey(c)));
    }

    [Fact]
    public void Options_ExposeEveryCatalog()
    {
        var options = TrainingMapper.BuildOptions();

        Assert.Equal(TrainingIntensityLevels.All.Length, options.IntensityLevels.Count);
        Assert.Equal(TrainingTypes.All.Length, options.TrainingTypes.Count);
        Assert.All(options.TrainingTypes, t => Assert.NotNull(t.IntensityLevel));
        Assert.Equal(TrackSurfaces.All.Length, options.Surfaces.Count);
        Assert.Equal(TrainingPlanTypes.All.Length, options.PlanTypes.Count);
        Assert.Equal(TrainingPlanStatuses.All.Length, options.PlanStatuses.Count);
        Assert.Equal(TrainingSessionStatuses.All.Length, options.SessionStatuses.Count);
    }

    [Theory]
    [InlineData("2026-10-12", "2026-10-12", "Thứ Hai")]
    [InlineData("2026-10-13", "2026-10-12", "Thứ Ba")]
    [InlineData("2026-10-16", "2026-10-12", "Thứ Sáu")]
    [InlineData("2026-10-18", "2026-10-12", "Chủ Nhật")]
    [InlineData("2026-10-19", "2026-10-19", "Thứ Hai")]
    public void WeekStartOf_ReturnsMonday(string date, string expectedMonday, string label)
    {
        var day = DateOnly.Parse(date);

        Assert.Equal(DateOnly.Parse(expectedMonday), TrainingRules.WeekStartOf(day));
        Assert.Equal(label, TrainingRules.DayOfWeekLabel(day));
    }

    [Fact]
    public void ServiceResult_AsFailure_KeepsCodeAndDetails()
    {
        var failed = ServiceResult<int>.Fail(ServiceErrorCodes.HorseNotFit, "msg", ["a", "b"]);

        var converted = failed.AsFailure<string>();

        Assert.False(converted.Success);
        Assert.Equal(ServiceErrorCodes.HorseNotFit, converted.ErrorCode);
        Assert.Equal("msg", converted.ErrorMessage);
        Assert.Equal(["a", "b"], converted.Details);
    }
}
