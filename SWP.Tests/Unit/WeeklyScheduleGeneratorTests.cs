using SWP.Application.Training;
using SWP.Domain.Common;

namespace SWP.Tests.Unit;

public class WeeklyScheduleGeneratorTests
{
    private static readonly DateOnly Monday = new(2026, 10, 12);

    private static PhaseSpec Phase(
        string intensity,
        int sessionsPerWeek = 5,
        DateOnly? start = null,
        DateOnly? end = null,
        decimal distanceKm = 4m,
        int durationMinutes = 45,
        string? surface = null,
        int id = 1,
        string name = "Giai đoạn") =>
        new(id, name, start ?? Monday, end ?? Monday.AddDays(27), intensity, sessionsPerWeek, distanceKm, durationMinutes, surface);

    private static WeeklyGenerationInput Input(
        params PhaseSpec[] phases) => new()
    {
        WeekStart = Monday,
        Phases = phases,
        MaxAllowedIntensity = TrainingIntensityLevels.High,
        Today = Monday,
    };

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void DayPatterns_HaveRequestedCount_AndAlwaysRestOnSunday(int sessionsPerWeek)
    {
        var days = WeeklyScheduleGenerator.DayPatterns[sessionsPerWeek];

        Assert.Equal(sessionsPerWeek, days.Length);
        Assert.Equal(sessionsPerWeek, days.Distinct().Count());
        Assert.DoesNotContain(6, days);
        Assert.All(days, d => Assert.InRange(d, 0, 5));
    }

    [Fact]
    public void HighPhase_AlternatesEasyAndHardDays()
    {
        var output = WeeklyScheduleGenerator.Generate(Input(Phase(TrainingIntensityLevels.High, surface: TrackSurfaces.Turf)));

        Assert.Equal(5, output.Sessions.Count);
        Assert.Equal(
            [Monday, Monday.AddDays(1), Monday.AddDays(2), Monday.AddDays(4), Monday.AddDays(5)],
            output.Sessions.Select(s => s.Date).ToArray());
        Assert.Equal(
            [TrainingTypes.Canter, TrainingTypes.Gallop, TrainingTypes.Canter, TrainingTypes.Gallop, TrainingTypes.Canter],
            output.Sessions.Select(s => s.TrainingType).ToArray());
        Assert.All(output.Sessions, s =>
        {
            Assert.Equal(4m, s.DistanceKm);
            Assert.Equal(45, s.DurationMinutes);
            Assert.Equal(TrackSurfaces.Turf, s.Surface);
            Assert.Equal(new TimeOnly(6, 0), s.StartTime);
            Assert.StartsWith("Giai đoạn - ", s.Title);
        });
        Assert.Empty(output.Warnings);
        Assert.Empty(output.SkippedDays);
    }

    [Theory]
    [InlineData(TrainingIntensityLevels.Medium, TrainingTypes.Canter, TrainingTypes.Trot)]
    [InlineData(TrainingIntensityLevels.Low, TrainingTypes.Trot, TrainingTypes.Walk)]
    [InlineData(TrainingIntensityLevels.Recovery, TrainingTypes.Walk, TrainingTypes.Walk)]
    public void SessionTypes_MatchPhaseIntensity(string intensity, string primary, string secondary)
    {
        var output = WeeklyScheduleGenerator.Generate(Input(Phase(intensity, sessionsPerWeek: 2)));

        Assert.Equal([secondary, primary], output.Sessions.Select(s => s.TrainingType).ToArray());
        Assert.All(output.Sessions, s => Assert.True(TrainingIntensityLevels.IsWithin(s.IntensityLevel, intensity)));
    }

    [Fact]
    public void SingleSessionPerWeek_IsThePrimaryWorkout()
    {
        var output = WeeklyScheduleGenerator.Generate(Input(Phase(TrainingIntensityLevels.High, sessionsPerWeek: 1)));

        var session = Assert.Single(output.Sessions);
        Assert.Equal(TrainingTypes.Gallop, session.TrainingType);
        Assert.Equal(Monday.AddDays(2), session.Date);
    }

    [Fact]
    public void FitnessCap_LowersIntensityAndVolume_WithWarning()
    {
        var input = Input(Phase(TrainingIntensityLevels.High, distanceKm: 5m, durationMinutes: 60, name: "Tăng tốc")) with
        {
            MaxAllowedIntensity = TrainingIntensityLevels.Low,
        };

        var output = WeeklyScheduleGenerator.Generate(input);

        Assert.Equal(5, output.Sessions.Count);
        Assert.All(output.Sessions, s =>
        {
            Assert.Contains(s.TrainingType, new[] { TrainingTypes.Trot, TrainingTypes.Walk });
            Assert.Equal(3.0m, s.DistanceKm);
            Assert.Equal(35, s.DurationMinutes);
        });
        Assert.Contains("Tăng tốc", Assert.Single(output.Warnings));
    }

    [Fact]
    public void FitnessCap_KeepsPhaseVolume_WhenItIsAlreadySmaller()
    {
        var input = Input(Phase(TrainingIntensityLevels.High, distanceKm: 1.5m, durationMinutes: 20)) with
        {
            MaxAllowedIntensity = TrainingIntensityLevels.Recovery,
        };

        var output = WeeklyScheduleGenerator.Generate(input);

        Assert.All(output.Sessions, s =>
        {
            Assert.Equal(TrainingTypes.Walk, s.TrainingType);
            Assert.Equal(1.5m, s.DistanceKm);
            Assert.Equal(20, s.DurationMinutes);
        });
    }

    [Fact]
    public void HorseNotAllowedToTrain_ProducesNoSessions()
    {
        var output = WeeklyScheduleGenerator.Generate(Input(Phase(TrainingIntensityLevels.Low)) with
        {
            MaxAllowedIntensity = null,
        });

        Assert.Empty(output.Sessions);
        Assert.Single(output.Warnings);
    }

    [Fact]
    public void PastDays_AreSkipped()
    {
        var output = WeeklyScheduleGenerator.Generate(Input(Phase(TrainingIntensityLevels.Medium)) with
        {
            Today = Monday.AddDays(2),
        });

        Assert.Equal([Monday.AddDays(2), Monday.AddDays(4), Monday.AddDays(5)], output.Sessions.Select(s => s.Date).ToArray());
        Assert.Equal([Monday, Monday.AddDays(1)], output.SkippedDays.Select(d => d.Date).ToArray());
        Assert.All(output.SkippedDays, d => Assert.Equal("Ngày đã qua.", d.Reason));
    }

    [Fact]
    public void OccupiedDays_AreSkipped_AndAlternationStaysStable()
    {
        var output = WeeklyScheduleGenerator.Generate(Input(Phase(TrainingIntensityLevels.High)) with
        {
            OccupiedDates = new HashSet<DateOnly> { Monday.AddDays(1) },
        });

        Assert.Equal(4, output.Sessions.Count);
        Assert.DoesNotContain(output.Sessions, s => s.Date == Monday.AddDays(1));
        Assert.Equal(TrainingTypes.Gallop, output.Sessions.Single(s => s.Date == Monday.AddDays(4)).TrainingType);
        Assert.Equal("Ngựa đã có lịch tập trong ngày.", Assert.Single(output.SkippedDays).Reason);
    }

    [Fact]
    public void PhaseBoundaryMidWeek_UsesThePhaseCoveringEachDay()
    {
        var first = Phase(TrainingIntensityLevels.Low, 6, Monday.AddDays(-7), Monday.AddDays(2), id: 1, name: "A");
        var second = Phase(TrainingIntensityLevels.High, 6, Monday.AddDays(3), Monday.AddDays(20), id: 2, name: "B");

        var output = WeeklyScheduleGenerator.Generate(Input(second, first));

        Assert.Equal(6, output.Sessions.Count);
        Assert.All(output.Sessions.Where(s => s.Date <= Monday.AddDays(2)), s => Assert.Equal(1, s.PhaseId));
        Assert.All(output.Sessions.Where(s => s.Date > Monday.AddDays(2)), s => Assert.Equal(2, s.PhaseId));
        Assert.Equal(output.Sessions.Select(s => s.Date).OrderBy(d => d), output.Sessions.Select(s => s.Date));
    }

    [Fact]
    public void PhaseEndingBeforeWeek_ContributesNothing()
    {
        var output = WeeklyScheduleGenerator.Generate(Input(
            Phase(TrainingIntensityLevels.High, start: Monday.AddDays(-14), end: Monday.AddDays(-1))));

        Assert.Empty(output.Sessions);
        Assert.Empty(output.SkippedDays);
    }

    [Fact]
    public void WeekStart_IsNormalisedToMonday()
    {
        var output = WeeklyScheduleGenerator.Generate(Input(Phase(TrainingIntensityLevels.Low)) with
        {
            WeekStart = Monday.AddDays(3),
        });

        Assert.Equal(Monday, output.WeekStart);
        Assert.Contains(output.Sessions, s => s.Date == Monday);
    }

    [Fact]
    public void CustomStartTime_IsApplied()
    {
        var output = WeeklyScheduleGenerator.Generate(Input(Phase(TrainingIntensityLevels.Low)) with
        {
            StartTime = new TimeOnly(15, 30),
        });

        Assert.All(output.Sessions, s => Assert.Equal(new TimeOnly(15, 30), s.StartTime));
    }

    [Theory]
    [InlineData(TrainingTypes.Walk, TrackSurfaces.Pool, TrackSurfaces.Sand)]
    [InlineData(TrainingTypes.Canter, TrackSurfaces.Turf, TrackSurfaces.Turf)]
    [InlineData(TrainingTypes.Canter, null, TrackSurfaces.Dirt)]
    [InlineData(TrainingTypes.Gallop, "invalid", TrackSurfaces.Turf)]
    [InlineData(TrainingTypes.Swimming, TrackSurfaces.Sand, TrackSurfaces.Pool)]
    [InlineData(TrainingTypes.Trot, "synthetic", TrackSurfaces.Synthetic)]
    public void ResolveSurface_RespectsPreferenceWhenCompatible(string trainingType, string? preferred, string expected)
    {
        Assert.Equal(expected, WeeklyScheduleGenerator.ResolveSurface(trainingType, preferred));
    }
}
