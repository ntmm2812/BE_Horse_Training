using SWP.Application.DTOs.Training;
using SWP.Application.Training;
using SWP.Domain.Common;
using SWP.Tests.Support;

namespace SWP.Tests.Unit;

public class TrainingValidationTests
{
    private static readonly DateOnly Today = new(2026, 10, 12);

    private static string? Validate(params TrainingPhaseRequestDto[] phases) =>
        TrainingValidation.ValidatePhases(phases, Today, DateTime.UtcNow, out _);

    private static TrainingPhaseRequestDto ValidPhase() =>
        TestData.Phase("Nền tảng", Today, Today.AddDays(13), TrainingIntensityLevels.Low);

    [Theory]
    [InlineData("06:30", 6, 30)]
    [InlineData("6:05", 6, 5)]
    [InlineData(" 17:45 ", 17, 45)]
    public void TryParseTime_AcceptsHourMinute(string value, int hour, int minute)
    {
        Assert.True(TrainingValidation.TryParseTime(value, TrainingRules.DefaultStartTime, out var time));
        Assert.Equal(new TimeOnly(hour, minute), time);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void TryParseTime_EmptyUsesFallback(string? value)
    {
        Assert.True(TrainingValidation.TryParseTime(value, new TimeOnly(5, 15), out var time));
        Assert.Equal(new TimeOnly(5, 15), time);
    }

    [Theory]
    [InlineData("25:00")]
    [InlineData("abc")]
    [InlineData("06:30:00")]
    [InlineData("6h30")]
    public void TryParseTime_RejectsInvalid(string value)
    {
        Assert.False(TrainingValidation.TryParseTime(value, TrainingRules.DefaultStartTime, out _));
    }

    [Fact]
    public void ValidatePhases_NormalisesSortsAndNumbers()
    {
        var later = TestData.Phase(" Tăng tốc ", Today.AddDays(14), Today.AddDays(27), "high", surface: "turf");
        var earlier = TestData.Phase("Nền tảng", Today, Today.AddDays(13), "LOW");

        var error = TrainingValidation.ValidatePhases([later, earlier], Today, DateTime.UtcNow, out var phases);

        Assert.Null(error);
        Assert.Equal(["Nền tảng", "Tăng tốc"], phases.Select(p => p.PhaseName).ToArray());
        Assert.Equal([1, 2], phases.Select(p => p.PhaseOrder).ToArray());
        Assert.Equal([TrainingIntensityLevels.Low, TrainingIntensityLevels.High], phases.Select(p => p.IntensityLevel).ToArray());
        Assert.Equal(TrackSurfaces.Turf, phases[1].Surface);
        Assert.Null(phases[0].Surface);
    }

    [Fact]
    public void ValidatePhases_RequiresAtLeastOnePhase()
    {
        Assert.NotNull(TrainingValidation.ValidatePhases(null, Today, DateTime.UtcNow, out _));
        Assert.NotNull(Validate());
    }

    [Fact]
    public void ValidatePhases_RejectsTooManyPhases()
    {
        var phases = Enumerable.Range(0, TrainingRules.MaxPhasesPerPlan + 1)
            .Select(i => TestData.Phase($"P{i}", Today.AddDays(i * 7), Today.AddDays(i * 7 + 6), TrainingIntensityLevels.Low))
            .ToArray();

        Assert.Contains("tối đa", Validate(phases));
    }

    [Fact]
    public void ValidatePhases_RejectsOverlap()
    {
        var error = Validate(
            TestData.Phase("A", Today, Today.AddDays(10), TrainingIntensityLevels.Low),
            TestData.Phase("B", Today.AddDays(10), Today.AddDays(20), TrainingIntensityLevels.Medium));

        Assert.Contains("trùng ngày", error);
    }

    [Fact]
    public void ValidatePhases_AllowsGapBetweenPhases()
    {
        var error = Validate(
            TestData.Phase("A", Today, Today.AddDays(6), TrainingIntensityLevels.Low),
            TestData.Phase("B", Today.AddDays(14), Today.AddDays(20), TrainingIntensityLevels.Medium));

        Assert.Null(error);
    }

    [Fact]
    public void ValidatePhases_RejectsPlanLongerThanOneYear()
    {
        var error = Validate(TestData.Phase("A", Today, Today.AddDays(TrainingRules.MaxPlanDays), TrainingIntensityLevels.Low));

        Assert.Contains("tối đa", error);
    }

    public static TheoryData<Action<TrainingPhaseRequestDto>, string> InvalidPhaseCases => new()
    {
        { p => p.PhaseName = " ", "tên giai đoạn" },
        { p => p.PhaseName = new string('x', 101), "100 ký tự" },
        { p => p.Goal = new string('x', 501), "500 ký tự" },
        { p => p.StartDate = p.EndDate.AddDays(1), "ngày bắt đầu" },
        { p => p.StartDate = Today.AddDays(-1), "quá khứ" },
        { p => p.IntensityLevel = "Extreme", "cường độ" },
        { p => p.SessionsPerWeek = 0, "số buổi/tuần" },
        { p => p.SessionsPerWeek = 7, "số buổi/tuần" },
        { p => p.TargetDistanceKm = 0, "cự ly" },
        { p => p.TargetDistanceKm = 15.5m, "cự ly" },
        { p => p.SessionDurationMinutes = 4, "thời lượng" },
        { p => p.SessionDurationMinutes = 241, "thời lượng" },
        { p => p.Surface = "Ice", "mặt sân" },
    };

    [Theory]
    [MemberData(nameof(InvalidPhaseCases))]
    public void ValidatePhases_RejectsInvalidField(Action<TrainingPhaseRequestDto> mutate, string expectedFragment)
    {
        var phase = ValidPhase();
        mutate(phase);

        var error = Validate(phase);

        Assert.NotNull(error);
        Assert.StartsWith("Giai đoạn 1:", error);
        Assert.Contains(expectedFragment, error);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    public void ValidatePhases_AcceptsSessionsPerWeekBounds(int sessionsPerWeek)
    {
        var phase = ValidPhase();
        phase.SessionsPerWeek = sessionsPerWeek;

        Assert.Null(Validate(phase));
    }

    [Fact]
    public void ValidatePlanHeader_NormalisesPlanType()
    {
        var error = TrainingValidation.ValidatePlanHeader("Giáo án", null, null, "racepreparation", out var planType);

        Assert.Null(error);
        Assert.Equal(TrainingPlanTypes.RacePreparation, planType);
    }

    [Theory]
    [InlineData(null, null, null, null, "tên giáo án")]
    [InlineData("  ", null, null, null, "tên giáo án")]
    [InlineData("x", null, null, "Unknown", "Loại giáo án")]
    public void ValidatePlanHeader_RejectsInvalid(string? name, string? objective, string? notes, string? type, string fragment)
    {
        var error = TrainingValidation.ValidatePlanHeader(name, objective, notes, type, out _);

        Assert.Contains(fragment, error);
    }

    [Fact]
    public void ValidatePlanHeader_RejectsTooLongFields()
    {
        Assert.NotNull(TrainingValidation.ValidatePlanHeader(new string('x', 151), null, null, null, out _));
        Assert.NotNull(TrainingValidation.ValidatePlanHeader("x", new string('x', 501), null, null, out _));
        Assert.NotNull(TrainingValidation.ValidatePlanHeader("x", null, new string('x', 1001), null, out _));
    }
}
