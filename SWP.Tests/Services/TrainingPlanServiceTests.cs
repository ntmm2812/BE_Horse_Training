using SWP.Application.Common;
using SWP.Application.DTOs.Training;
using SWP.Application.Training;
using SWP.Domain.Common;
using SWP.Tests.Support;

namespace SWP.Tests.Services;

public class TrainingPlanServiceTests : IDisposable
{
    private readonly TrainingTestContext _t = new();

    public void Dispose() => _t.Dispose();

    private static DateOnly Today => TestData.Today;

    // ---------- Lập giáo án ----------

    [Fact]
    public async Task Create_ValidPlan_IsSavedAsDraftWithDerivedFields()
    {
        var horseId = _t.AddHorse("Thunder");

        var result = await _t.PlanService().CreateAsync(TestData.ThreePhasePlan(horseId), _t.Ids.TrainerId);

        Assert.True(result.Success, result.ErrorMessage);
        var plan = result.Data!;
        Assert.Equal(TrainingPlanStatuses.Draft, plan.Status);
        Assert.Equal("Thunder", plan.HorseName);
        Assert.Equal(Today, plan.StartDate);
        Assert.Equal(Today.AddDays(41), plan.EndDate);
        Assert.Equal(TrainingIntensityLevels.High, plan.IntensityLevel);
        Assert.Equal(TrainingPlanTypes.RacePreparation, plan.TrainingType);
        Assert.Equal(_t.Ids.TrainerId, plan.CreatedByUserId);
        Assert.Equal(3, plan.PhaseCount);
        Assert.Equal(["Nền tảng", "Phát triển", "Tăng tốc"], plan.Phases.Select(p => p.PhaseName).ToArray());
        Assert.Equal([1, 2, 3], plan.Phases.Select(p => p.PhaseOrder).ToArray());
        Assert.Empty(plan.Sessions);
        Assert.Equal(0, plan.Progress.TotalSessions);
    }

    [Fact]
    public async Task Create_UnknownHorse_IsNotFound()
    {
        var result = await _t.PlanService().CreateAsync(TestData.ThreePhasePlan(9999), _t.Ids.TrainerId);

        Assert.Equal(ServiceErrorCodes.NotFound, result.ErrorCode);
    }

    [Fact]
    public async Task Create_MissingHorseId_IsInvalid()
    {
        var result = await _t.PlanService().CreateAsync(TestData.ThreePhasePlan(0), _t.Ids.TrainerId);

        Assert.Equal(ServiceErrorCodes.Validation, result.ErrorCode);
    }

    [Fact]
    public async Task Create_WithoutPhases_IsInvalid()
    {
        var horseId = _t.AddHorse("Thunder");
        var request = TestData.ThreePhasePlan(horseId);
        request.Phases.Clear();

        var result = await _t.PlanService().CreateAsync(request, _t.Ids.TrainerId);

        Assert.Equal(ServiceErrorCodes.Validation, result.ErrorCode);
        Assert.Contains("ít nhất một giai đoạn", result.ErrorMessage);
    }

    [Fact]
    public async Task Create_WithInvalidPlanType_IsInvalid()
    {
        var horseId = _t.AddHorse("Thunder");
        var request = TestData.ThreePhasePlan(horseId);
        request.TrainingType = "Cardio";

        var result = await _t.PlanService().CreateAsync(request, _t.Ids.TrainerId);

        Assert.Equal(ServiceErrorCodes.Validation, result.ErrorCode);
    }

    [Fact]
    public async Task Create_ForQuarantinedHorse_IsRejectedByFitness()
    {
        var horseId = _t.AddHorse("Quarantine", healthStatus: HorseHealthStatuses.CachLy);

        var result = await _t.PlanService().CreateAsync(TestData.ThreePhasePlan(horseId), _t.Ids.TrainerId);

        Assert.Equal(ServiceErrorCodes.HorseNotFit, result.ErrorCode);
        Assert.NotEmpty(result.Details);
    }

    [Fact]
    public async Task Create_PhaseHeavierThanFitness_IsRejected_NamingThePhase()
    {
        var horseId = _t.AddHorse("Watch", healthStatus: HorseHealthStatuses.CanTheoDoi);

        var result = await _t.PlanService().CreateAsync(TestData.ThreePhasePlan(horseId), _t.Ids.TrainerId);

        Assert.Equal(ServiceErrorCodes.HorseNotFit, result.ErrorCode);
        Assert.Contains("Phát triển", result.ErrorMessage);
    }

    [Fact]
    public async Task Create_PhasesWithinFitness_ForLimitedHorse_Succeeds()
    {
        var horseId = _t.AddHorse("Injured", healthStatus: HorseHealthStatuses.ChanThuong);
        var request = new CreateTrainingPlanRequestDto
        {
            HorseId = horseId,
            PlanName = "Phục hồi sau chấn thương",
            TrainingType = TrainingPlanTypes.Rehabilitation,
            Phases = [TestData.Phase("Phục hồi", Today, Today.AddDays(20), TrainingIntensityLevels.Recovery, 3, 1.5m, 25)],
        };

        var result = await _t.PlanService().CreateAsync(request, _t.Ids.TrainerId);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(TrainingIntensityLevels.Recovery, result.Data!.IntensityLevel);
    }

    // ---------- Cập nhật / xóa ----------

    [Fact]
    public async Task Update_Draft_ReplacesPhases()
    {
        var horseId = _t.AddHorse("Thunder");
        var created = await _t.PlanService().CreateAsync(TestData.ThreePhasePlan(horseId), _t.Ids.TrainerId);
        var update = new UpdateTrainingPlanRequestDto
        {
            PlanName = "Giáo án sửa",
            Objective = "Mục tiêu mới",
            Phases = [TestData.Phase("Một giai đoạn", Today.AddDays(7), Today.AddDays(34), TrainingIntensityLevels.Medium)],
        };

        var result = await _t.PlanService().UpdateAsync(created.Data!.PlanId, update);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal("Giáo án sửa", result.Data!.PlanName);
        Assert.Equal("Mục tiêu mới", result.Data.Objective);
        Assert.Null(result.Data.TrainingType);
        Assert.Equal("Một giai đoạn", Assert.Single(result.Data.Phases).PhaseName);
        Assert.Equal(Today.AddDays(7), result.Data.StartDate);
        Assert.Equal(Today.AddDays(34), result.Data.EndDate);
        Assert.Equal(TrainingIntensityLevels.Medium, result.Data.IntensityLevel);
        Assert.NotNull(result.Data.UpdatedAt);
        Assert.Equal(1, _t.Db(db => db.TrainingPlanPhases.Count()));
    }

    [Fact]
    public async Task Update_WithoutPhases_KeepsExistingPhases()
    {
        var horseId = _t.AddHorse("Thunder");
        var created = await _t.PlanService().CreateAsync(TestData.ThreePhasePlan(horseId), _t.Ids.TrainerId);

        var result = await _t.PlanService().UpdateAsync(
            created.Data!.PlanId,
            new UpdateTrainingPlanRequestDto { PlanName = "Đổi tên", Notes = "  " });

        Assert.True(result.Success);
        Assert.Equal(3, result.Data!.Phases.Count);
        Assert.Null(result.Data.Notes);
    }

    [Fact]
    public async Task Update_ActivePlan_AllowsHeaderButNotPhases()
    {
        var horseId = _t.AddHorse("Thunder");
        var planId = await _t.CreateActivePlanAsync(horseId);

        var header = await _t.PlanService().UpdateAsync(planId, new UpdateTrainingPlanRequestDto { PlanName = "Đổi tên" });
        var phases = await _t.PlanService().UpdateAsync(planId, new UpdateTrainingPlanRequestDto
        {
            PlanName = "Đổi tên",
            Phases = [TestData.Phase("X", Today, Today.AddDays(6), TrainingIntensityLevels.Low)],
        });

        Assert.True(header.Success);
        Assert.Equal(ServiceErrorCodes.InvalidStatus, phases.ErrorCode);
    }

    [Fact]
    public async Task Update_ClosedPlan_IsRejected()
    {
        var horseId = _t.AddHorse("Thunder");
        var planId = await _t.CreateActivePlanAsync(horseId);
        await _t.PlanService().CompleteAsync(planId);

        var result = await _t.PlanService().UpdateAsync(planId, new UpdateTrainingPlanRequestDto { PlanName = "x" });

        Assert.Equal(ServiceErrorCodes.InvalidStatus, result.ErrorCode);
    }

    [Fact]
    public async Task Update_UnknownPlan_IsNotFound()
    {
        var result = await _t.PlanService().UpdateAsync(9999, new UpdateTrainingPlanRequestDto { PlanName = "x" });

        Assert.Equal(ServiceErrorCodes.NotFound, result.ErrorCode);
    }

    [Fact]
    public async Task Delete_Draft_RemovesPlanAndPhases()
    {
        var horseId = _t.AddHorse("Thunder");
        var created = await _t.PlanService().CreateAsync(TestData.ThreePhasePlan(horseId), _t.Ids.TrainerId);

        var result = await _t.PlanService().DeleteAsync(created.Data!.PlanId);

        Assert.True(result.Success);
        Assert.Equal(0, _t.Db(db => db.TrainingPlans.Count()));
        Assert.Equal(0, _t.Db(db => db.TrainingPlanPhases.Count()));
    }

    [Fact]
    public async Task Delete_ActivePlan_IsRejected()
    {
        var horseId = _t.AddHorse("Thunder");
        var planId = await _t.CreateActivePlanAsync(horseId);

        var result = await _t.PlanService().DeleteAsync(planId);

        Assert.Equal(ServiceErrorCodes.InvalidStatus, result.ErrorCode);
        Assert.Equal(ServiceErrorCodes.NotFound, (await _t.PlanService().DeleteAsync(9999)).ErrorCode);
    }

    // ---------- Vòng đời giáo án ----------

    [Fact]
    public async Task Activate_Draft_BecomesActive()
    {
        var horseId = _t.AddHorse("Thunder");
        var created = await _t.PlanService().CreateAsync(TestData.ThreePhasePlan(horseId), _t.Ids.TrainerId);

        var result = await _t.PlanService().ActivateAsync(created.Data!.PlanId);

        Assert.True(result.Success);
        Assert.Equal(TrainingPlanStatuses.Active, result.Data!.Status);
    }

    [Fact]
    public async Task Activate_Twice_IsInvalidStatus()
    {
        var horseId = _t.AddHorse("Thunder");
        var planId = await _t.CreateActivePlanAsync(horseId);

        var result = await _t.PlanService().ActivateAsync(planId);

        Assert.Equal(ServiceErrorCodes.InvalidStatus, result.ErrorCode);
    }

    [Fact]
    public async Task Activate_SecondPlanForSameHorse_Conflicts()
    {
        var horseId = _t.AddHorse("Thunder");
        await _t.CreateActivePlanAsync(horseId);
        var second = await _t.PlanService().CreateAsync(TestData.ThreePhasePlan(horseId, "Giáo án 2"), _t.Ids.TrainerId);

        var result = await _t.PlanService().ActivateAsync(second.Data!.PlanId);

        Assert.Equal(ServiceErrorCodes.Conflict, result.ErrorCode);
    }

    [Fact]
    public async Task Activate_PlansOfDifferentHorses_DoNotConflict()
    {
        await _t.CreateActivePlanAsync(_t.AddHorse("A"));

        var planId = await _t.CreateActivePlanAsync(_t.AddHorse("B"));

        Assert.True(planId > 0);
    }

    [Fact]
    public async Task Activate_AfterHorseIsQuarantined_IsRejected()
    {
        var horseId = _t.AddHorse("Thunder");
        var created = await _t.PlanService().CreateAsync(TestData.ThreePhasePlan(horseId), _t.Ids.TrainerId);
        _t.Db(db => TestData.SetHealthStatus(db, horseId, HorseHealthStatuses.CachLy));

        var result = await _t.PlanService().ActivateAsync(created.Data!.PlanId);

        Assert.Equal(ServiceErrorCodes.HorseNotFit, result.ErrorCode);
    }

    [Fact]
    public async Task Activate_ExpiredPlan_IsInvalid()
    {
        var horseId = _t.AddHorse("Thunder");
        var created = await _t.PlanService().CreateAsync(TestData.ThreePhasePlan(horseId), _t.Ids.TrainerId);
        _t.Clock.Advance(TimeSpan.FromDays(60));

        var result = await _t.PlanService().ActivateAsync(created.Data!.PlanId);

        Assert.Equal(ServiceErrorCodes.Validation, result.ErrorCode);
    }

    [Fact]
    public async Task Complete_CancelsRemainingScheduledSessions()
    {
        var horseId = _t.AddHorse("Thunder");
        var planId = await _t.CreateActivePlanAsync(horseId);
        var completedId = _t.Db(db => TestData.AddSession(db, planId, Today.AddDays(-1), TrainingTypes.Trot, TrainingSessionStatuses.Completed, performanceScore: 8));
        var scheduledId = _t.Db(db => TestData.AddSession(db, planId, Today.AddDays(2), TrainingTypes.Trot, TrainingSessionStatuses.Scheduled));

        var result = await _t.PlanService().CompleteAsync(planId);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(TrainingPlanStatuses.Completed, result.Data!.Status);
        var sessions = result.Data.Sessions.ToDictionary(s => s.SessionId);
        Assert.Equal(TrainingSessionStatuses.Completed, sessions[completedId].Status);
        Assert.Equal(TrainingSessionStatuses.Cancelled, sessions[scheduledId].Status);
        Assert.Equal("Hủy do giáo án đã kết thúc.", sessions[scheduledId].Result);
    }

    [Fact]
    public async Task Complete_WithSessionInProgress_IsBlocked()
    {
        var horseId = _t.AddHorse("Thunder");
        var planId = await _t.CreateActivePlanAsync(horseId);
        _t.Db(db => TestData.AddSession(db, planId, Today, TrainingTypes.Trot, TrainingSessionStatuses.InProgress));

        var result = await _t.PlanService().CompleteAsync(planId);

        Assert.Equal(ServiceErrorCodes.InvalidStatus, result.ErrorCode);
        Assert.Contains("đang diễn ra", result.ErrorMessage);
    }

    [Fact]
    public async Task Complete_DraftPlan_IsInvalidStatus()
    {
        var horseId = _t.AddHorse("Thunder");
        var created = await _t.PlanService().CreateAsync(TestData.ThreePhasePlan(horseId), _t.Ids.TrainerId);

        var result = await _t.PlanService().CompleteAsync(created.Data!.PlanId);

        Assert.Equal(ServiceErrorCodes.InvalidStatus, result.ErrorCode);
    }

    [Fact]
    public async Task Cancel_DraftOrActive_Succeeds_ButNotTwice()
    {
        var horseId = _t.AddHorse("Thunder");
        var draft = await _t.PlanService().CreateAsync(TestData.ThreePhasePlan(horseId), _t.Ids.TrainerId);
        var activeId = await _t.CreateActivePlanAsync(_t.AddHorse("Other"));
        var scheduledId = _t.Db(db => TestData.AddSession(db, activeId, Today.AddDays(1), TrainingTypes.Trot, TrainingSessionStatuses.Scheduled));

        var cancelDraft = await _t.PlanService().CancelAsync(draft.Data!.PlanId);
        var cancelActive = await _t.PlanService().CancelAsync(activeId);
        var cancelAgain = await _t.PlanService().CancelAsync(activeId);

        Assert.Equal(TrainingPlanStatuses.Cancelled, cancelDraft.Data!.Status);
        Assert.Equal(TrainingPlanStatuses.Cancelled, cancelActive.Data!.Status);
        Assert.Equal(TrainingSessionStatuses.Cancelled, cancelActive.Data.Sessions.Single(s => s.SessionId == scheduledId).Status);
        Assert.Equal(ServiceErrorCodes.InvalidStatus, cancelAgain.ErrorCode);
    }

    [Fact]
    public async Task CancelledPlan_FreesHorseForNewActivePlan()
    {
        var horseId = _t.AddHorse("Thunder");
        var first = await _t.CreateActivePlanAsync(horseId);
        await _t.PlanService().CancelAsync(first);

        var second = await _t.CreateActivePlanAsync(horseId);

        Assert.NotEqual(first, second);
    }

    // ---------- Tra cứu ----------

    [Fact]
    public async Task Search_FiltersByHorseAndStatus()
    {
        var a = _t.AddHorse("A");
        var b = _t.AddHorse("B");
        await _t.CreateActivePlanAsync(a);
        await _t.PlanService().CreateAsync(TestData.ThreePhasePlan(a, "Nháp A"), _t.Ids.TrainerId);
        await _t.PlanService().CreateAsync(TestData.ThreePhasePlan(b, "Nháp B"), _t.Ids.TrainerId);

        var all = await _t.PlanService().SearchAsync(null, null);
        var ofA = await _t.PlanService().SearchAsync(a, null);
        var drafts = await _t.PlanService().SearchAsync(null, "draft");
        var activeOfA = await _t.PlanService().SearchAsync(a, TrainingPlanStatuses.Active);

        Assert.Equal(3, all.Data!.Count);
        Assert.Equal(2, ofA.Data!.Count);
        Assert.Equal(2, drafts.Data!.Count);
        Assert.All(drafts.Data, p => Assert.Equal(TrainingPlanStatuses.Draft, p.Status));
        Assert.Equal(TrainingPlanStatuses.Active, Assert.Single(activeOfA.Data!).Status);
    }

    [Fact]
    public async Task Search_InvalidStatus_IsInvalid()
    {
        var result = await _t.PlanService().SearchAsync(null, "Running");

        Assert.Equal(ServiceErrorCodes.Validation, result.ErrorCode);
    }

    [Fact]
    public async Task Detail_ComputesProgress()
    {
        var horseId = _t.AddHorse("Thunder");
        var planId = await _t.CreateActivePlanAsync(horseId);
        _t.Db(db =>
        {
            TestData.AddSession(db, planId, Today.AddDays(-2), TrainingTypes.Trot, TrainingSessionStatuses.Completed, actualDistanceKm: 3.5m, performanceScore: 8);
            TestData.AddSession(db, planId, Today.AddDays(-1), TrainingTypes.Trot, TrainingSessionStatuses.Completed, distanceKm: 2.5m, performanceScore: 7);
            TestData.AddSession(db, planId, Today.AddDays(1), TrainingTypes.Trot, TrainingSessionStatuses.Scheduled);
            TestData.AddSession(db, planId, Today.AddDays(2), TrainingTypes.Trot, TrainingSessionStatuses.Cancelled);
        });

        var result = await _t.PlanService().GetDetailAsync(planId);

        var progress = result.Data!.Progress;
        Assert.Equal(4, progress.TotalSessions);
        Assert.Equal(2, progress.CompletedSessions);
        Assert.Equal(1, progress.CancelledSessions);
        Assert.Equal(1, progress.UpcomingSessions);
        Assert.Equal(66.67m, progress.CompletionRate);
        Assert.Equal(7.5m, progress.AveragePerformanceScore);
        Assert.Equal(6m, progress.TotalActualDistanceKm);
        Assert.Equal(result.Data.Sessions.Select(s => s.TrainingDate).OrderBy(d => d), result.Data.Sessions.Select(s => s.TrainingDate));
        Assert.All(result.Data.Sessions, s => Assert.Equal("Thunder", s.HorseName));
    }

    [Fact]
    public async Task Detail_UnknownPlan_IsNotFound()
    {
        Assert.Equal(ServiceErrorCodes.NotFound, (await _t.PlanService().GetDetailAsync(9999)).ErrorCode);
    }

    // ---------- Gợi ý giáo án theo thể trạng ----------

    [Fact]
    public async Task Suggestion_ForReadyHorse_StartsTodayOnMonday_AndCanBeSubmitted()
    {
        var horseId = _t.AddHorse("Thunder");

        var suggestion = await _t.PlanService().GetSuggestionAsync(horseId, null, null);

        Assert.True(suggestion.Success);
        var data = suggestion.Data!;
        Assert.True(data.CanCreatePlan);
        Assert.Equal(Today, data.StartDate);
        Assert.Equal(Today.AddDays(8 * 7 - 1), data.EndDate);
        Assert.Equal(4, data.Phases.Count);
        Assert.Equal(TrainingPlanTypes.RacePreparation, data.SuggestedTrainingType);
        Assert.Equal(FitnessReadiness.Ready, data.Fitness.Readiness);

        var created = await _t.PlanService().CreateAsync(new CreateTrainingPlanRequestDto
        {
            HorseId = horseId,
            PlanName = data.SuggestedPlanName!,
            TrainingType = data.SuggestedTrainingType,
            Phases = data.Phases.ToList(),
        }, _t.Ids.TrainerId);
        Assert.True(created.Success, created.ErrorMessage);
    }

    [Fact]
    public async Task Suggestion_MidWeek_DefaultsToNextMonday()
    {
        var horseId = _t.AddHorse("Thunder");
        _t.Clock.Advance(TimeSpan.FromDays(2));

        var suggestion = await _t.PlanService().GetSuggestionAsync(horseId, null, 4);

        Assert.Equal(TestData.NextMonday, suggestion.Data!.StartDate);
        Assert.Equal(TestData.NextMonday.AddDays(27), suggestion.Data.EndDate);
    }

    [Fact]
    public async Task Suggestion_ForInjuredHorse_IsRecoveryOnly()
    {
        var horseId = _t.AddHorse("Injured", healthStatus: HorseHealthStatuses.ChanThuong);

        var suggestion = await _t.PlanService().GetSuggestionAsync(horseId, Today.AddDays(3), 4);

        Assert.All(suggestion.Data!.Phases, p => Assert.Equal(TrainingIntensityLevels.Recovery, p.IntensityLevel));
        Assert.Equal(TrainingPlanTypes.Rehabilitation, suggestion.Data.SuggestedTrainingType);
        Assert.Equal(Today.AddDays(3), suggestion.Data.StartDate);
    }

    [Fact]
    public async Task Suggestion_ForQuarantinedHorse_CannotCreatePlan()
    {
        var horseId = _t.AddHorse("Quarantine", healthStatus: HorseHealthStatuses.CachLy);

        var suggestion = await _t.PlanService().GetSuggestionAsync(horseId, null, null);

        Assert.True(suggestion.Success);
        Assert.False(suggestion.Data!.CanCreatePlan);
        Assert.Empty(suggestion.Data.Phases);
        Assert.Null(suggestion.Data.SuggestedPlanName);
        Assert.Null(suggestion.Data.EndDate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(27)]
    public async Task Suggestion_InvalidWeeks_IsInvalid(int weeks)
    {
        var horseId = _t.AddHorse("Thunder");

        var result = await _t.PlanService().GetSuggestionAsync(horseId, null, weeks);

        Assert.Equal(ServiceErrorCodes.Validation, result.ErrorCode);
    }

    [Fact]
    public async Task Suggestion_PastStartDate_IsInvalid_AndUnknownHorseNotFound()
    {
        var horseId = _t.AddHorse("Thunder");

        Assert.Equal(ServiceErrorCodes.Validation, (await _t.PlanService().GetSuggestionAsync(horseId, Today.AddDays(-1), 4)).ErrorCode);
        Assert.Equal(ServiceErrorCodes.NotFound, (await _t.PlanService().GetSuggestionAsync(9999, null, 4)).ErrorCode);
    }

    // ---------- Xếp lịch tuần ----------

    private static GenerateWeeklyScheduleRequestDto Week(
        DateOnly weekStart,
        bool preview = false,
        bool replace = false,
        string? startTime = null,
        int? assignee = null) => new()
    {
        WeekStartDate = weekStart,
        PreviewOnly = preview,
        ReplaceExisting = replace,
        DefaultStartTime = startTime,
        AssignedToUserId = assignee,
    };

    [Fact]
    public async Task GenerateWeek_ActivePlan_PersistsSessionsFromPhase()
    {
        var horseId = _t.AddHorse("Thunder");
        var planId = await _t.CreateActivePlanAsync(horseId);

        var result = await _t.PlanService().GenerateWeeklyScheduleAsync(planId, Week(Today));

        Assert.True(result.Success, result.ErrorMessage);
        var data = result.Data!;
        Assert.False(data.PreviewOnly);
        Assert.Equal(Today, data.WeekStartDate);
        Assert.Equal(Today.AddDays(6), data.WeekEndDate);
        Assert.Equal(4, data.CreatedCount);
        Assert.Equal(
            [Today, Today.AddDays(1), Today.AddDays(3), Today.AddDays(4)],
            data.Sessions.Select(s => s.TrainingDate).ToArray());
        Assert.All(data.Sessions, s =>
        {
            Assert.True(s.SessionId > 0);
            Assert.Equal("Nền tảng", s.PhaseName);
            Assert.Equal(TrackSurfaces.Sand, s.Surface);
            Assert.Equal("06:00", s.StartTime);
            Assert.Equal(3m, s.DistanceKm);
            Assert.Equal(35, s.DurationMinutes);
            Assert.True(TrainingIntensityLevels.IsWithin(s.IntensityLevel, TrainingIntensityLevels.Low));
            Assert.Equal(TrainingSessionStatuses.Scheduled, s.Status);
        });
        Assert.Equal(4, _t.Db(db => db.TrainingSchedules.Count(s => s.PlanId == planId && s.PhaseId != null)));
    }

    [Fact]
    public async Task GenerateWeek_HighPhase_UsesGallopOnTurf()
    {
        var horseId = _t.AddHorse("Thunder");
        var planId = await _t.CreateActivePlanAsync(horseId);

        var result = await _t.PlanService().GenerateWeeklyScheduleAsync(planId, Week(Today.AddDays(28)));

        Assert.Equal(5, result.Data!.CreatedCount);
        Assert.Equal(2, result.Data.Sessions.Count(s => s.TrainingType == TrainingTypes.Gallop));
        Assert.All(result.Data.Sessions, s => Assert.Equal(TrackSurfaces.Turf, s.Surface));
    }

    [Fact]
    public async Task GenerateWeek_IsIdempotent_WithoutReplace()
    {
        var horseId = _t.AddHorse("Thunder");
        var planId = await _t.CreateActivePlanAsync(horseId);
        await _t.PlanService().GenerateWeeklyScheduleAsync(planId, Week(Today));

        var second = await _t.PlanService().GenerateWeeklyScheduleAsync(planId, Week(Today));

        Assert.Equal(0, second.Data!.CreatedCount);
        Assert.Equal(4, second.Data.SkippedDays.Count);
        Assert.Equal(4, _t.Db(db => db.TrainingSchedules.Count()));
    }

    [Fact]
    public async Task GenerateWeek_Replace_RegeneratesScheduledButKeepsStartedSessions()
    {
        var horseId = _t.AddHorse("Thunder");
        var planId = await _t.CreateActivePlanAsync(horseId);
        var first = await _t.PlanService().GenerateWeeklyScheduleAsync(planId, Week(Today));
        var todaySession = first.Data!.Sessions.Single(s => s.TrainingDate == Today).SessionId;
        await _t.SessionService().StartAsync(todaySession);

        var result = await _t.PlanService().GenerateWeeklyScheduleAsync(planId, Week(Today, replace: true, startTime: "07:15"));

        Assert.Equal(3, result.Data!.RemovedCount);
        Assert.Equal(3, result.Data.CreatedCount);
        Assert.All(result.Data.Sessions, s => Assert.Equal("07:15", s.StartTime));
        Assert.Equal(4, _t.Db(db => db.TrainingSchedules.Count()));
        Assert.Equal(TrainingSessionStatuses.InProgress, _t.Db(db => db.TrainingSchedules.Single(s => s.ScheduleId == todaySession).Status));
    }

    [Fact]
    public async Task GenerateWeek_Preview_DoesNotSave_AndWorksForDraft()
    {
        var horseId = _t.AddHorse("Thunder");
        var draft = await _t.PlanService().CreateAsync(TestData.ThreePhasePlan(horseId), _t.Ids.TrainerId);

        var preview = await _t.PlanService().GenerateWeeklyScheduleAsync(draft.Data!.PlanId, Week(Today, preview: true));
        var persist = await _t.PlanService().GenerateWeeklyScheduleAsync(draft.Data.PlanId, Week(Today));

        Assert.True(preview.Success);
        Assert.True(preview.Data!.PreviewOnly);
        Assert.Equal(4, preview.Data.Sessions.Count);
        Assert.Equal(0, preview.Data.CreatedCount);
        Assert.All(preview.Data.Sessions, s => Assert.Equal(0, s.SessionId));
        Assert.Equal(ServiceErrorCodes.InvalidStatus, persist.ErrorCode);
        Assert.Equal(0, _t.Db(db => db.TrainingSchedules.Count()));
    }

    [Fact]
    public async Task GenerateWeek_MidWeek_SkipsPastDays()
    {
        var horseId = _t.AddHorse("Thunder");
        var planId = await _t.CreateActivePlanAsync(horseId);
        _t.Clock.Advance(TimeSpan.FromDays(2));

        var result = await _t.PlanService().GenerateWeeklyScheduleAsync(planId, Week(Today.AddDays(2)));

        Assert.Equal([Today.AddDays(3), Today.AddDays(4)], result.Data!.Sessions.Select(s => s.TrainingDate).ToArray());
        Assert.Equal(2, result.Data.SkippedDays.Count(d => d.Reason == "Ngày đã qua."));
    }

    [Fact]
    public async Task GenerateWeek_InjuredAfterActivation_CapsToRecovery()
    {
        var horseId = _t.AddHorse("Thunder");
        var planId = await _t.CreateActivePlanAsync(horseId);
        _t.Db(db => TestData.SetHealthStatus(db, horseId, HorseHealthStatuses.ChanThuong));

        var result = await _t.PlanService().GenerateWeeklyScheduleAsync(planId, Week(Today.AddDays(28)));

        Assert.True(result.Success);
        Assert.All(result.Data!.Sessions, s => Assert.Equal(TrainingTypes.Walk, s.TrainingType));
        Assert.Contains(result.Data.Warnings, w => w.Contains("Tăng tốc"));
    }

    [Fact]
    public async Task GenerateWeek_QuarantinedHorse_IsRejected()
    {
        var horseId = _t.AddHorse("Thunder");
        var planId = await _t.CreateActivePlanAsync(horseId);
        _t.Db(db => TestData.SetHealthStatus(db, horseId, HorseHealthStatuses.CachLy));

        var result = await _t.PlanService().GenerateWeeklyScheduleAsync(planId, Week(Today));

        Assert.Equal(ServiceErrorCodes.HorseNotFit, result.ErrorCode);
        Assert.Equal(0, _t.Db(db => db.TrainingSchedules.Count()));
    }

    [Fact]
    public async Task GenerateWeek_OutsidePlan_IsInvalid()
    {
        var horseId = _t.AddHorse("Thunder");
        var planId = await _t.CreateActivePlanAsync(horseId);

        var after = await _t.PlanService().GenerateWeeklyScheduleAsync(planId, Week(Today.AddDays(70)));
        var unset = await _t.PlanService().GenerateWeeklyScheduleAsync(planId, Week(default));

        Assert.Equal(ServiceErrorCodes.Validation, after.ErrorCode);
        Assert.Equal(ServiceErrorCodes.Validation, unset.ErrorCode);
    }

    [Fact]
    public async Task GenerateWeek_AssignsStaff_AndValidatesAssignee()
    {
        var horseId = _t.AddHorse("Thunder");
        var planId = await _t.CreateActivePlanAsync(horseId);

        var pending = await _t.PlanService().GenerateWeeklyScheduleAsync(planId, Week(Today, assignee: _t.Ids.PendingGroomId));
        var owner = await _t.PlanService().GenerateWeeklyScheduleAsync(planId, Week(Today, assignee: _t.Ids.OwnerId));
        var badTime = await _t.PlanService().GenerateWeeklyScheduleAsync(planId, Week(Today, startTime: "6h"));
        var groom = await _t.PlanService().GenerateWeeklyScheduleAsync(planId, Week(Today, assignee: _t.Ids.GroomId));

        Assert.Equal(ServiceErrorCodes.Validation, pending.ErrorCode);
        Assert.Equal(ServiceErrorCodes.Validation, owner.ErrorCode);
        Assert.Equal(ServiceErrorCodes.Validation, badTime.ErrorCode);
        Assert.All(groom.Data!.Sessions, s =>
        {
            Assert.Equal(_t.Ids.GroomId, s.AssignedToUserId);
            Assert.Equal("Nhân viên Chăm sóc", s.AssignedToName);
        });
    }

    [Fact]
    public async Task GenerateWeek_SkipsDaysWhereHorseTrainsUnderAnotherPlan()
    {
        var horseId = _t.AddHorse("Thunder");
        var planId = await _t.CreateActivePlanAsync(horseId);
        _t.Db(db =>
        {
            var (otherPlan, _) = TestData.AddPlan(db, horseId, Today, Today.AddDays(6), TrainingPlanStatuses.Completed);
            TestData.AddSession(db, otherPlan, Today.AddDays(1), TrainingTypes.Trot, TrainingSessionStatuses.Scheduled);
        });

        var result = await _t.PlanService().GenerateWeeklyScheduleAsync(planId, Week(Today));

        Assert.Equal(3, result.Data!.CreatedCount);
        Assert.DoesNotContain(result.Data.Sessions, s => s.TrainingDate == Today.AddDays(1));
    }

    [Fact]
    public async Task GenerateWeek_UnknownPlan_IsNotFound()
    {
        var result = await _t.PlanService().GenerateWeeklyScheduleAsync(9999, Week(Today));

        Assert.Equal(ServiceErrorCodes.NotFound, result.ErrorCode);
    }
}
