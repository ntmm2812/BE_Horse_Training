using SWP.Application.DTOs.Training;
using SWP.Application.Training;
using SWP.Domain.Common;

namespace SWP.Tests.Unit;

public class TrainingPlanSuggesterTests
{
    private static readonly DateOnly Start = new(2026, 10, 19);

    [Fact]
    public void HorseThatCannotTrain_GetsNoPlan()
    {
        var suggestion = TrainingPlanSuggester.Suggest(null, 5, Start, 8);

        Assert.False(suggestion.CanCreatePlan);
        Assert.Null(suggestion.PlanType);
        Assert.Empty(suggestion.Phases);
        Assert.NotEmpty(suggestion.Rationale);
    }

    [Fact]
    public void ReadyHorse_GetsProgressiveRacePreparation()
    {
        var suggestion = TrainingPlanSuggester.Suggest(TrainingIntensityLevels.High, 5, Start, 8);

        Assert.True(suggestion.CanCreatePlan);
        Assert.Equal(TrainingPlanTypes.RacePreparation, suggestion.PlanType);
        Assert.Equal(
            [TrainingIntensityLevels.Low, TrainingIntensityLevels.Medium, TrainingIntensityLevels.High, TrainingIntensityLevels.Medium],
            suggestion.Phases.Select(p => p.IntensityLevel).ToArray());
        Assert.Equal(Start, suggestion.Phases[0].StartDate);
        Assert.Equal(Start.AddDays(8 * 7 - 1), suggestion.Phases[^1].EndDate);
    }

    [Theory]
    [InlineData(TrainingIntensityLevels.Medium, TrainingPlanTypes.Endurance, new[] { "Low", "Medium" })]
    [InlineData(TrainingIntensityLevels.Low, TrainingPlanTypes.Conditioning, new[] { "Recovery", "Low" })]
    [InlineData(TrainingIntensityLevels.Recovery, TrainingPlanTypes.Rehabilitation, new[] { "Recovery" })]
    public void LimitedHorse_GetsPhasesWithinItsLevel(string recommended, string planType, string[] intensities)
    {
        var suggestion = TrainingPlanSuggester.Suggest(recommended, 6, Start, 8);

        Assert.Equal(planType, suggestion.PlanType);
        Assert.Equal(intensities, suggestion.Phases.Select(p => p.IntensityLevel).ToArray());
        Assert.All(suggestion.Phases, p => Assert.True(TrainingIntensityLevels.IsWithin(p.IntensityLevel, recommended)));
    }

    [Theory]
    [InlineData(TrainingIntensityLevels.High, 1)]
    [InlineData(TrainingIntensityLevels.High, 3)]
    [InlineData(TrainingIntensityLevels.High, 8)]
    [InlineData(TrainingIntensityLevels.High, 26)]
    [InlineData(TrainingIntensityLevels.Medium, 5)]
    [InlineData(TrainingIntensityLevels.Low, 2)]
    [InlineData(TrainingIntensityLevels.Recovery, 4)]
    public void Phases_AreContiguous_AndCoverEveryWeek(string recommended, int weeks)
    {
        var phases = TrainingPlanSuggester.Suggest(recommended, 5, Start, weeks).Phases;

        Assert.Equal(Start, phases[0].StartDate);
        Assert.Equal(Start.AddDays(weeks * 7 - 1), phases[^1].EndDate);
        for (var i = 1; i < phases.Count; i++)
        {
            Assert.Equal(phases[i - 1].EndDate.AddDays(1), phases[i].StartDate);
        }

        Assert.All(phases, p => Assert.Equal(0, (p.EndDate.DayNumber - p.StartDate.DayNumber + 1) % 7));
    }

    [Fact]
    public void ShortPlan_DropsLaterPhases_AndExplainsWhy()
    {
        var suggestion = TrainingPlanSuggester.Suggest(TrainingIntensityLevels.High, 5, Start, 2);

        Assert.Equal(2, suggestion.Phases.Count);
        Assert.Contains(suggestion.Rationale, r => r.Contains("lược bớt"));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(15)]
    [InlineData(20)]
    public void YoungOrSeniorHorse_GetsShorterDistances(int age)
    {
        var normal = TrainingPlanSuggester.Suggest(TrainingIntensityLevels.Medium, 6, Start, 8);
        var reduced = TrainingPlanSuggester.Suggest(TrainingIntensityLevels.Medium, age, Start, 8);

        for (var i = 0; i < normal.Phases.Count; i++)
        {
            Assert.Equal(Math.Round(normal.Phases[i].TargetDistanceKm * 0.8m, 1), reduced.Phases[i].TargetDistanceKm);
        }

        Assert.Contains(reduced.Rationale, r => r.Contains("giảm 20%"));
    }

    [Theory]
    [InlineData(TrainingIntensityLevels.High)]
    [InlineData(TrainingIntensityLevels.Medium)]
    [InlineData(TrainingIntensityLevels.Low)]
    [InlineData(TrainingIntensityLevels.Recovery)]
    public void SuggestedPhases_PassPlanValidation(string recommended)
    {
        var phases = TrainingPlanSuggester.Suggest(recommended, 1, Start, 12).Phases
            .Select(p => new TrainingPhaseRequestDto
            {
                PhaseName = p.PhaseName,
                Goal = p.Goal,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                IntensityLevel = p.IntensityLevel,
                SessionsPerWeek = p.SessionsPerWeek,
                TargetDistanceKm = p.TargetDistanceKm,
                SessionDurationMinutes = p.SessionDurationMinutes,
                Surface = p.Surface,
            })
            .ToList();

        var error = TrainingValidation.ValidatePhases(phases, Start, DateTime.UtcNow, out _);

        Assert.Null(error);
    }

    [Theory]
    [InlineData(new[] { 0.25, 0.35, 0.25, 0.15 }, 8, new[] { 2, 3, 2, 1 })]
    [InlineData(new[] { 0.25, 0.35, 0.25, 0.15 }, 2, new[] { 1, 1, 0, 0 })]
    [InlineData(new[] { 0.4, 0.6 }, 5, new[] { 2, 3 })]
    [InlineData(new[] { 1.0 }, 7, new[] { 7 })]
    public void AllocateWeeks_UsesLargestRemainder(double[] ratios, int weeks, int[] expected)
    {
        var result = TrainingPlanSuggester.AllocateWeeks(ratios.Select(r => (decimal)r).ToArray(), weeks);

        Assert.Equal(expected, result);
        Assert.Equal(weeks, result.Sum());
    }
}
