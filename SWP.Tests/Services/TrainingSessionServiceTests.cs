using SWP.Application.Common;
using SWP.Application.DTOs.Training;
using SWP.Domain.Common;
using SWP.Tests.Support;

namespace SWP.Tests.Services;

public class TrainingSessionServiceTests : IDisposable
{
    private readonly TrainingTestContext _t = new();
    private readonly int _horseId;
    private readonly int _planId;

    public TrainingSessionServiceTests()
    {
        _horseId = _t.AddHorse("Thunder");
        _planId = _t.CreateActivePlanAsync(_horseId).GetAwaiter().GetResult();
    }

    public void Dispose() => _t.Dispose();

    private static DateOnly Today => TestData.Today;

    /// <summary> Ngày thuộc giai đoạn "Tăng tốc" (High) của giáo án mẫu. </summary>
    private static DateOnly HighPhaseDay => Today.AddDays(30);

    private async Task<TrainingSessionDto> CreateAsync(SaveTrainingSessionRequestDto request)
    {
        var result = await _t.SessionService().CreateAsync(_planId, request);
        Assert.True(result.Success, result.ErrorMessage);
        return result.Data!;
    }

    private async Task<TrainingSessionDto> CreateStartedTodayAsync(string type = TrainingTypes.Trot)
    {
        var session = await CreateAsync(TestData.Session(Today, type));
        var started = await _t.SessionService().StartAsync(session.SessionId);
        Assert.True(started.Success, started.ErrorMessage);
        return started.Data!;
    }

    // ---------- Xếp lịch thủ công ----------

    [Fact]
    public async Task Create_AppliesPhaseDefaults()
    {
        var session = await CreateAsync(TestData.Session(Today.AddDays(2), "trot"));

        Assert.True(session.SessionId > 0);
        Assert.Equal(TrainingSessionStatuses.Scheduled, session.Status);
        Assert.Equal(TrainingTypes.Trot, session.TrainingType);
        Assert.Equal(TrainingIntensityLevels.Low, session.IntensityLevel);
        Assert.Equal(TrackSurfaces.Sand, session.Surface);
        Assert.Equal("06:00", session.StartTime);
        Assert.Equal("Nền tảng - Nước kiệu", session.SessionTitle);
        Assert.Equal("Nền tảng", session.PhaseName);
        Assert.Equal("Thunder", session.HorseName);
        Assert.Equal(_horseId, session.HorseId);
        Assert.Equal(_planId, session.PlanId);
    }

    [Fact]
    public async Task Create_WithExplicitValues_InHighPhase()
    {
        var request = TestData.Session(HighPhaseDay, TrainingTypes.Gallop, 2.4m, 30, "dirt", "16:45", _t.Ids.GroomId);
        request.SessionTitle = "Phi nước đại 2400m";
        request.Notes = "Đeo cảm biến nhịp tim";

        var session = await CreateAsync(request);

        Assert.Equal("Tăng tốc", session.PhaseName);
        Assert.Equal(TrackSurfaces.Dirt, session.Surface);
        Assert.Equal("16:45", session.StartTime);
        Assert.Equal(2.4m, session.DistanceKm);
        Assert.Equal(30, session.DurationMinutes);
        Assert.Equal("Phi nước đại 2400m", session.SessionTitle);
        Assert.Equal("Đeo cảm biến nhịp tim", session.Notes);
        Assert.Equal(_t.Ids.GroomId, session.AssignedToUserId);
        Assert.Equal("Nhân viên Chăm sóc", session.AssignedToName);
    }

    [Fact]
    public async Task Create_OnDraftPlan_IsInvalidStatus()
    {
        var draft = await _t.PlanService().CreateAsync(TestData.ThreePhasePlan(_t.AddHorse("Draft")), _t.Ids.TrainerId);

        var result = await _t.SessionService().CreateAsync(draft.Data!.PlanId, TestData.Session(Today, TrainingTypes.Trot));

        Assert.Equal(ServiceErrorCodes.InvalidStatus, result.ErrorCode);
    }

    [Fact]
    public async Task Create_UnknownPlan_IsNotFound()
    {
        var result = await _t.SessionService().CreateAsync(9999, TestData.Session(Today, TrainingTypes.Trot));

        Assert.Equal(ServiceErrorCodes.NotFound, result.ErrorCode);
    }

    public static TheoryData<string> InvalidSessionCases => new()
    {
        "past-date", "after-plan", "missing-date", "unknown-type", "type-above-phase", "unknown-surface",
        "swim-on-sand", "trot-in-pool", "zero-distance", "long-distance", "short-duration", "long-duration",
        "bad-time", "long-title", "long-notes", "pending-assignee", "owner-assignee", "missing-assignee",
    };

    [Theory]
    [MemberData(nameof(InvalidSessionCases))]
    public async Task Create_InvalidInput_IsRejected(string scenario)
    {
        var request = TestData.Session(Today.AddDays(1), TrainingTypes.Trot);
        switch (scenario)
        {
            case "past-date": request.TrainingDate = Today.AddDays(-1); break;
            case "after-plan": request.TrainingDate = Today.AddDays(60); break;
            case "missing-date": request.TrainingDate = default; break;
            case "unknown-type": request.TrainingType = "Cardio"; break;
            case "type-above-phase": request.TrainingType = TrainingTypes.Canter; break;
            case "unknown-surface": request.Surface = "Ice"; break;
            case "swim-on-sand": request.TrainingType = TrainingTypes.Swimming; request.Surface = TrackSurfaces.Sand; break;
            case "trot-in-pool": request.Surface = TrackSurfaces.Pool; break;
            case "zero-distance": request.DistanceKm = 0; break;
            case "long-distance": request.DistanceKm = 20; break;
            case "short-duration": request.DurationMinutes = 3; break;
            case "long-duration": request.DurationMinutes = 300; break;
            case "bad-time": request.StartTime = "25:61"; break;
            case "long-title": request.SessionTitle = new string('x', 151); break;
            case "long-notes": request.Notes = new string('x', 1001); break;
            case "pending-assignee": request.AssignedToUserId = _t.Ids.PendingGroomId; break;
            case "owner-assignee": request.AssignedToUserId = _t.Ids.OwnerId; break;
            case "missing-assignee": request.AssignedToUserId = 9999; break;
        }

        var result = await _t.SessionService().CreateAsync(_planId, request);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorCodes.Validation, result.ErrorCode);
        Assert.Equal(0, _t.Db(db => db.TrainingSchedules.Count()));
    }

    [Fact]
    public async Task Create_SwimmingDefaultsToPool()
    {
        var session = await CreateAsync(TestData.Session(Today.AddDays(1), TrainingTypes.Swimming));

        Assert.Equal(TrackSurfaces.Pool, session.Surface);
    }

    [Fact]
    public async Task Create_DateInGapBetweenPhases_IsRejected()
    {
        var horseId = _t.AddHorse("Gap");
        var plan = await _t.PlanService().CreateAsync(new CreateTrainingPlanRequestDto
        {
            HorseId = horseId,
            PlanName = "Có tuần nghỉ",
            Phases =
            [
                TestData.Phase("Tuần 1", Today, Today.AddDays(6), TrainingIntensityLevels.Low),
                TestData.Phase("Tuần 3", Today.AddDays(14), Today.AddDays(20), TrainingIntensityLevels.Low),
            ],
        }, _t.Ids.TrainerId);
        await _t.PlanService().ActivateAsync(plan.Data!.PlanId);

        var result = await _t.SessionService().CreateAsync(plan.Data.PlanId, TestData.Session(Today.AddDays(9), TrainingTypes.Trot));

        Assert.Equal(ServiceErrorCodes.Validation, result.ErrorCode);
        Assert.Contains("không thuộc giai đoạn", result.ErrorMessage);
    }

    [Fact]
    public async Task Create_WhenVetLockedHorse_RecoveryWorkIsAlsoBlocked()
    {
        _t.Db(db => TestData.LockHorse(db, _horseId, "Nghi viêm khớp"));

        var trot = await _t.SessionService().CreateAsync(_planId, TestData.Session(Today.AddDays(1), TrainingTypes.Trot));
        var walk = await _t.SessionService().CreateAsync(_planId, TestData.Session(Today.AddDays(1), TrainingTypes.Walk));

        Assert.Equal(ServiceErrorCodes.HorseNotFit, trot.ErrorCode);
        Assert.Contains(trot.Details, d => d.Contains("Nghi viêm khớp"));
        Assert.Equal(ServiceErrorCodes.HorseNotFit, walk.ErrorCode);
    }

    [Fact]
    public async Task Create_ThirdSessionSameDay_Conflicts_ButCancelledOnesDoNotCount()
    {
        var day = Today.AddDays(1);
        var first = await CreateAsync(TestData.Session(day, TrainingTypes.Trot, startTime: "06:00"));
        await CreateAsync(TestData.Session(day, TrainingTypes.Walk, startTime: "16:00"));

        var third = await _t.SessionService().CreateAsync(_planId, TestData.Session(day, TrainingTypes.Walk, startTime: "18:00"));
        await _t.SessionService().CancelAsync(first.SessionId, new CancelTrainingSessionRequestDto { Reason = "Đổi giờ" });
        var afterCancel = await _t.SessionService().CreateAsync(_planId, TestData.Session(day, TrainingTypes.Walk, startTime: "18:00"));

        Assert.Equal(ServiceErrorCodes.Conflict, third.ErrorCode);
        Assert.True(afterCancel.Success, afterCancel.ErrorMessage);
    }

    // ---------- Chỉnh sửa / phân công ----------

    [Fact]
    public async Task Update_ChangesScheduledSession()
    {
        var session = await CreateAsync(TestData.Session(Today.AddDays(2), TrainingTypes.Trot));

        var result = await _t.SessionService().UpdateAsync(
            session.SessionId,
            TestData.Session(Today.AddDays(3), TrainingTypes.Walk, 2m, 25, startTime: "09:00"));

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(Today.AddDays(3), result.Data!.TrainingDate);
        Assert.Equal(TrainingTypes.Walk, result.Data.TrainingType);
        Assert.Equal("09:00", result.Data.StartTime);
        Assert.Equal(2m, result.Data.DistanceKm);
        Assert.Equal(25, result.Data.DurationMinutes);
        Assert.Equal("Nền tảng - Đi bộ", result.Data.SessionTitle);
        var stored = _t.Db(db => db.TrainingSchedules.Single(s => s.ScheduleId == session.SessionId));
        Assert.Equal(Today.AddDays(3).ToDateTime(TimeOnly.MinValue), stored.TrainingDate);
    }

    [Fact]
    public async Task Update_DoesNotCountItselfTowardsDailyLimit()
    {
        var day = Today.AddDays(1);
        var first = await CreateAsync(TestData.Session(day, TrainingTypes.Trot));
        await CreateAsync(TestData.Session(day, TrainingTypes.Walk, startTime: "16:00"));

        var result = await _t.SessionService().UpdateAsync(first.SessionId, TestData.Session(day, TrainingTypes.Trot, 3.5m));

        Assert.True(result.Success, result.ErrorMessage);
    }

    [Fact]
    public async Task Update_StartedSession_IsInvalidStatus_AndUnknownIsNotFound()
    {
        var started = await CreateStartedTodayAsync();

        var result = await _t.SessionService().UpdateAsync(started.SessionId, TestData.Session(Today, TrainingTypes.Walk));
        var missing = await _t.SessionService().UpdateAsync(9999, TestData.Session(Today, TrainingTypes.Walk));

        Assert.Equal(ServiceErrorCodes.InvalidStatus, result.ErrorCode);
        Assert.Equal(ServiceErrorCodes.NotFound, missing.ErrorCode);
    }

    [Fact]
    public async Task Assign_AndUnassign()
    {
        var session = await CreateAsync(TestData.Session(Today.AddDays(1), TrainingTypes.Trot));

        var assigned = await _t.SessionService().AssignAsync(session.SessionId, new AssignTrainingSessionRequestDto { AssignedToUserId = _t.Ids.GroomId });
        var trainer = await _t.SessionService().AssignAsync(session.SessionId, new AssignTrainingSessionRequestDto { AssignedToUserId = _t.Ids.TrainerId });
        var cleared = await _t.SessionService().AssignAsync(session.SessionId, new AssignTrainingSessionRequestDto { AssignedToUserId = null });

        Assert.Equal("Nhân viên Chăm sóc", assigned.Data!.AssignedToName);
        Assert.Equal(_t.Ids.TrainerId, trainer.Data!.AssignedToUserId);
        Assert.Null(cleared.Data!.AssignedToUserId);
        Assert.Null(_t.Db(db => db.TrainingSchedules.Single(s => s.ScheduleId == session.SessionId).AssignedToUserId));
    }

    [Fact]
    public async Task Assign_InvalidCases()
    {
        var session = await CreateAsync(TestData.Session(Today.AddDays(1), TrainingTypes.Trot));
        await _t.SessionService().CancelAsync(session.SessionId, new CancelTrainingSessionRequestDto { Reason = "x" });
        var open = await CreateAsync(TestData.Session(Today.AddDays(2), TrainingTypes.Trot));

        var closed = await _t.SessionService().AssignAsync(session.SessionId, new AssignTrainingSessionRequestDto { AssignedToUserId = _t.Ids.GroomId });
        var vet = await _t.SessionService().AssignAsync(open.SessionId, new AssignTrainingSessionRequestDto { AssignedToUserId = _t.Ids.VetId });
        var missing = await _t.SessionService().AssignAsync(9999, new AssignTrainingSessionRequestDto());

        Assert.Equal(ServiceErrorCodes.InvalidStatus, closed.ErrorCode);
        Assert.Equal(ServiceErrorCodes.Validation, vet.ErrorCode);
        Assert.Equal(ServiceErrorCodes.NotFound, missing.ErrorCode);
    }

    // ---------- Thực hiện buổi tập ----------

    [Fact]
    public async Task Start_TodaySession_RecordsActualStartTime()
    {
        var session = await CreateAsync(TestData.Session(Today, TrainingTypes.Trot, startTime: "06:00"));

        var result = await _t.SessionService().StartAsync(session.SessionId);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(TrainingSessionStatuses.InProgress, result.Data!.Status);
        Assert.Equal("08:00", result.Data.StartTime);
    }

    [Fact]
    public async Task Start_FutureSession_IsInvalid_AndStartingTwiceIsInvalidStatus()
    {
        var tomorrow = await CreateAsync(TestData.Session(Today.AddDays(1), TrainingTypes.Trot));
        var started = await CreateStartedTodayAsync();

        var future = await _t.SessionService().StartAsync(tomorrow.SessionId);
        var twice = await _t.SessionService().StartAsync(started.SessionId);
        var missing = await _t.SessionService().StartAsync(9999);

        Assert.Equal(ServiceErrorCodes.Validation, future.ErrorCode);
        Assert.Equal(ServiceErrorCodes.InvalidStatus, twice.ErrorCode);
        Assert.Equal(ServiceErrorCodes.NotFound, missing.ErrorCode);
    }

    [Fact]
    public async Task Start_AfterVetLocksHorse_IsBlocked()
    {
        var session = await CreateAsync(TestData.Session(Today, TrainingTypes.Trot));
        _t.Db(db => TestData.LockHorse(db, _horseId, "Khập khiễng chân sau"));

        var result = await _t.SessionService().StartAsync(session.SessionId);

        Assert.Equal(ServiceErrorCodes.HorseNotFit, result.ErrorCode);
        Assert.Equal(TrainingSessionStatuses.Scheduled, _t.Db(db => db.TrainingSchedules.Single(s => s.ScheduleId == session.SessionId).Status));
    }

    [Fact]
    public async Task Start_SessionOfInactivePlan_IsInvalidStatus()
    {
        var sessionId = _t.Db(db =>
        {
            var (planId, _) = TestData.AddPlan(db, _t.AddHorse("Old"), Today.AddDays(-30), Today.AddDays(30), TrainingPlanStatuses.Completed);
            return TestData.AddSession(db, planId, Today, TrainingTypes.Trot, TrainingSessionStatuses.Scheduled);
        });

        var result = await _t.SessionService().StartAsync(sessionId);

        Assert.Equal(ServiceErrorCodes.InvalidStatus, result.ErrorCode);
        Assert.Contains("không còn", result.ErrorMessage);
    }

    [Fact]
    public async Task Complete_RecordsMetricsScoreAndComment()
    {
        var started = await CreateStartedTodayAsync();
        _t.Clock.Advance(TimeSpan.FromMinutes(45));
        var request = TestData.Completion();
        request.TrainerComment = "  Ngựa giữ nhịp tốt, hồi sức nhanh.  ";

        var result = await _t.SessionService().CompleteAsync(started.SessionId, request);

        Assert.True(result.Success, result.ErrorMessage);
        var dto = result.Data!;
        Assert.Equal(TrainingSessionStatuses.Completed, dto.Status);
        Assert.Equal("08:45", dto.EndTime);
        Assert.Equal(3.2m, dto.ActualDistanceKm);
        Assert.Equal(42, dto.ActualDurationMinutes);
        Assert.Equal(8.5m, dto.PerformanceScore);
        Assert.Equal(120m, dto.AvgHeartRate);
        Assert.Equal(185m, dto.MaxHeartRate);
        Assert.Equal(28.5m, dto.AvgSpeedKmh);
        Assert.Equal("Hoàn thành tốt", dto.Result);
        Assert.Equal("Ngựa giữ nhịp tốt, hồi sức nhanh.", dto.TrainerComment);
        Assert.Equal("https://videos.example.com/trial-1.mp4", dto.TrialVideoUrl);

        var stored = _t.Db(db => db.TrainingSchedules.Single(s => s.ScheduleId == started.SessionId));
        Assert.Equal(TrainingSessionStatuses.Completed, stored.Status);
        Assert.Equal(8.5m, stored.PerformanceScore);
        Assert.Equal(3m, stored.DistanceKm);
    }

    [Fact]
    public async Task Complete_NotStarted_IsInvalidStatus()
    {
        var session = await CreateAsync(TestData.Session(Today, TrainingTypes.Trot));

        var result = await _t.SessionService().CompleteAsync(session.SessionId, TestData.Completion());

        Assert.Equal(ServiceErrorCodes.InvalidStatus, result.ErrorCode);
        Assert.Equal(ServiceErrorCodes.NotFound, (await _t.SessionService().CompleteAsync(9999, TestData.Completion())).ErrorCode);
    }

    public static TheoryData<string> InvalidCompletionCases => new()
    {
        "score-above-10", "negative-score", "zero-distance", "huge-distance", "zero-duration", "huge-duration",
        "low-heart-rate", "high-heart-rate", "max-below-avg", "zero-speed", "huge-speed", "no-comment",
        "long-comment", "long-result", "ftp-video", "relative-video",
    };

    [Theory]
    [MemberData(nameof(InvalidCompletionCases))]
    public async Task Complete_InvalidResult_IsRejected(string scenario)
    {
        var started = await CreateStartedTodayAsync();
        var request = TestData.Completion();
        switch (scenario)
        {
            case "score-above-10": request.PerformanceScore = 10.5m; break;
            case "negative-score": request.PerformanceScore = -1; break;
            case "zero-distance": request.ActualDistanceKm = 0; break;
            case "huge-distance": request.ActualDistanceKm = 51; break;
            case "zero-duration": request.ActualDurationMinutes = 0; break;
            case "huge-duration": request.ActualDurationMinutes = 301; break;
            case "low-heart-rate": request.AvgHeartRate = 15; break;
            case "high-heart-rate": request.MaxHeartRate = 300; break;
            case "max-below-avg": request.AvgHeartRate = 150; request.MaxHeartRate = 140; break;
            case "zero-speed": request.AvgSpeedKmh = 0; break;
            case "huge-speed": request.AvgSpeedKmh = 95; break;
            case "no-comment": request.TrainerComment = " "; break;
            case "long-comment": request.TrainerComment = new string('x', 1001); break;
            case "long-result": request.Result = new string('x', 501); break;
            case "ftp-video": request.TrialVideoUrl = "ftp://videos.example.com/a.mp4"; break;
            case "relative-video": request.TrialVideoUrl = "/videos/a.mp4"; break;
        }

        var result = await _t.SessionService().CompleteAsync(started.SessionId, request);

        Assert.Equal(ServiceErrorCodes.Validation, result.ErrorCode);
        Assert.Equal(TrainingSessionStatuses.InProgress, _t.Db(db => db.TrainingSchedules.Single(s => s.ScheduleId == started.SessionId).Status));
    }

    [Fact]
    public async Task Complete_BoundaryValues_AreAccepted()
    {
        var started = await CreateStartedTodayAsync();
        var request = TestData.Completion(score: 0);
        request.AvgHeartRate = null;
        request.MaxHeartRate = 260;
        request.AvgSpeedKmh = null;
        request.TrialVideoUrl = null;
        request.Result = null;

        var result = await _t.SessionService().CompleteAsync(started.SessionId, request);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(0m, result.Data!.PerformanceScore);
    }

    [Fact]
    public async Task CompletedHighIntensityWork_FeedsBackIntoFitnessAssessment()
    {
        _t.Clock.SetNow(TestData.Now.AddDays(28));
        foreach (var type in new[] { TrainingTypes.Gallop, TrainingTypes.Interval, TrainingTypes.Gallop })
        {
            var today = DateOnly.FromDateTime(_t.Clock.GetUtcNow().UtcDateTime);
            var session = await CreateAsync(TestData.Session(today, type, 2m, 30));
            Assert.True((await _t.SessionService().StartAsync(session.SessionId)).Success);
            Assert.True((await _t.SessionService().CompleteAsync(session.SessionId, TestData.Completion())).Success);
            _t.Clock.Advance(TimeSpan.FromDays(1));
        }

        var fitness = await _t.FitnessService().GetAssessmentAsync(_horseId);

        Assert.Equal(3, fitness!.RecentLoad.HighIntensitySessions);
        Assert.Equal(9.6m, fitness.RecentLoad.DistanceKm);
        Assert.Equal(TrainingIntensityLevels.High, fitness.MaxAllowedIntensity);
        Assert.Equal(TrainingIntensityLevels.Medium, fitness.RecommendedIntensity);
    }

    // ---------- Hủy / xóa ----------

    [Fact]
    public async Task Cancel_ScheduledAndInProgress()
    {
        var scheduled = await CreateAsync(TestData.Session(Today.AddDays(1), TrainingTypes.Trot));
        var started = await CreateStartedTodayAsync();

        var cancelScheduled = await _t.SessionService().CancelAsync(scheduled.SessionId, new CancelTrainingSessionRequestDto { Reason = " Trời mưa lớn " });
        var cancelStarted = await _t.SessionService().CancelAsync(started.SessionId, new CancelTrainingSessionRequestDto { Reason = "Ngựa bỏ ăn" });

        Assert.Equal(TrainingSessionStatuses.Cancelled, cancelScheduled.Data!.Status);
        Assert.Equal("Đã hủy: Trời mưa lớn", cancelScheduled.Data.Result);
        Assert.Null(cancelScheduled.Data.EndTime);
        Assert.Equal(TrainingSessionStatuses.Cancelled, cancelStarted.Data!.Status);
        Assert.Equal("08:00", cancelStarted.Data.EndTime);
    }

    [Fact]
    public async Task Cancel_InvalidCases()
    {
        var session = await CreateAsync(TestData.Session(Today.AddDays(1), TrainingTypes.Trot));
        var completed = await CreateStartedTodayAsync();
        await _t.SessionService().CompleteAsync(completed.SessionId, TestData.Completion());

        var noReason = await _t.SessionService().CancelAsync(session.SessionId, new CancelTrainingSessionRequestDto { Reason = "" });
        var longReason = await _t.SessionService().CancelAsync(session.SessionId, new CancelTrainingSessionRequestDto { Reason = new string('x', 451) });
        var done = await _t.SessionService().CancelAsync(completed.SessionId, new CancelTrainingSessionRequestDto { Reason = "x" });
        var missing = await _t.SessionService().CancelAsync(9999, new CancelTrainingSessionRequestDto { Reason = "x" });

        Assert.Equal(ServiceErrorCodes.Validation, noReason.ErrorCode);
        Assert.Equal(ServiceErrorCodes.Validation, longReason.ErrorCode);
        Assert.Equal(ServiceErrorCodes.InvalidStatus, done.ErrorCode);
        Assert.Equal(ServiceErrorCodes.NotFound, missing.ErrorCode);
    }

    [Fact]
    public async Task Delete_OnlyScheduledSessions()
    {
        var scheduled = await CreateAsync(TestData.Session(Today.AddDays(1), TrainingTypes.Trot));
        var started = await CreateStartedTodayAsync();

        var deleted = await _t.SessionService().DeleteAsync(scheduled.SessionId);
        var blocked = await _t.SessionService().DeleteAsync(started.SessionId);
        var missing = await _t.SessionService().DeleteAsync(9999);

        Assert.True(deleted.Success);
        Assert.Equal(ServiceErrorCodes.InvalidStatus, blocked.ErrorCode);
        Assert.Equal(ServiceErrorCodes.NotFound, missing.ErrorCode);
        Assert.False(_t.Db(db => db.TrainingSchedules.Any(s => s.ScheduleId == scheduled.SessionId)));
    }

    // ---------- Tra cứu ----------

    [Fact]
    public async Task Search_DefaultsToNextSevenDays_AndFilters()
    {
        var inRange = await CreateAsync(TestData.Session(Today.AddDays(6), TrainingTypes.Trot, assignedToUserId: _t.Ids.GroomId));
        await CreateAsync(TestData.Session(Today.AddDays(7), TrainingTypes.Trot));
        var cancelled = await CreateAsync(TestData.Session(Today.AddDays(1), TrainingTypes.Walk));
        await _t.SessionService().CancelAsync(cancelled.SessionId, new CancelTrainingSessionRequestDto { Reason = "x" });

        var defaults = await _t.SessionService().SearchAsync(new TrainingSessionQueryDto());
        var byStatus = await _t.SessionService().SearchAsync(new TrainingSessionQueryDto { Status = "cancelled" });
        var byAssignee = await _t.SessionService().SearchAsync(new TrainingSessionQueryDto { AssignedToUserId = _t.Ids.GroomId });
        var byOtherHorse = await _t.SessionService().SearchAsync(new TrainingSessionQueryDto { HorseId = 9999 });
        var byPlan = await _t.SessionService().SearchAsync(new TrainingSessionQueryDto { PlanId = _planId, To = Today.AddDays(10) });

        Assert.Equal([cancelled.SessionId, inRange.SessionId], defaults.Data!.Select(s => s.SessionId).ToArray());
        Assert.Equal(cancelled.SessionId, Assert.Single(byStatus.Data!).SessionId);
        Assert.Equal(inRange.SessionId, Assert.Single(byAssignee.Data!).SessionId);
        Assert.Empty(byOtherHorse.Data!);
        Assert.Equal(3, byPlan.Data!.Count);
    }

    [Fact]
    public async Task Search_InvalidRangeOrStatus_IsInvalid()
    {
        var reversed = await _t.SessionService().SearchAsync(new TrainingSessionQueryDto { From = Today, To = Today.AddDays(-1) });
        var tooLong = await _t.SessionService().SearchAsync(new TrainingSessionQueryDto { From = Today, To = Today.AddDays(62) });
        var longest = await _t.SessionService().SearchAsync(new TrainingSessionQueryDto { From = Today, To = Today.AddDays(61) });
        var badStatus = await _t.SessionService().SearchAsync(new TrainingSessionQueryDto { Status = "Done" });

        Assert.Equal(ServiceErrorCodes.Validation, reversed.ErrorCode);
        Assert.Equal(ServiceErrorCodes.Validation, tooLong.ErrorCode);
        Assert.True(longest.Success);
        Assert.Equal(ServiceErrorCodes.Validation, badStatus.ErrorCode);
    }

    [Fact]
    public async Task GetById_ReturnsSession()
    {
        var session = await CreateAsync(TestData.Session(Today.AddDays(1), TrainingTypes.Trot));

        var found = await _t.SessionService().GetByIdAsync(session.SessionId);

        Assert.Equal("Nền tảng", found.Data!.PhaseName);
        Assert.Equal("Thunder", found.Data.HorseName);
        Assert.Equal(ServiceErrorCodes.NotFound, (await _t.SessionService().GetByIdAsync(9999)).ErrorCode);
    }

    [Fact]
    public async Task MyAssignments_ShowOnlyOpenSessionsOfThatUser()
    {
        var mine = await CreateAsync(TestData.Session(Today.AddDays(1), TrainingTypes.Trot, assignedToUserId: _t.Ids.GroomId));
        var mineCancelled = await CreateAsync(TestData.Session(Today.AddDays(2), TrainingTypes.Trot, assignedToUserId: _t.Ids.GroomId));
        await CreateAsync(TestData.Session(Today.AddDays(3), TrainingTypes.Trot, assignedToUserId: _t.Ids.TrainerId));
        await _t.SessionService().CancelAsync(mineCancelled.SessionId, new CancelTrainingSessionRequestDto { Reason = "x" });

        var result = await _t.SessionService().GetMyAssignmentsAsync(_t.Ids.GroomId, null, null);
        var invalid = await _t.SessionService().GetMyAssignmentsAsync(_t.Ids.GroomId, Today, Today.AddDays(-2));

        Assert.Equal(mine.SessionId, Assert.Single(result.Data!).SessionId);
        Assert.Equal(ServiceErrorCodes.Validation, invalid.ErrorCode);
    }

    [Fact]
    public async Task AssignableStaff_AreActiveGroomsAndTrainers()
    {
        var staff = await _t.SessionService().GetAssignableStaffAsync();

        Assert.Equal(
            new[] { (_t.Ids.GroomId, RoleNames.Groom), (_t.Ids.TrainerId, RoleNames.HeadTrainer) },
            staff.Select(s => (s.UserId, s.RoleId)).ToArray());
    }

    // ---------- Lịch tuần của ngựa ----------

    [Fact]
    public async Task WeeklySchedule_ShowsSevenDaysWithSummary()
    {
        await _t.PlanService().GenerateWeeklyScheduleAsync(_planId, new GenerateWeeklyScheduleRequestDto { WeekStartDate = Today });
        var monday = (await _t.SessionService().SearchAsync(new TrainingSessionQueryDto { From = Today, To = Today })).Data!.Single();
        await _t.SessionService().StartAsync(monday.SessionId);
        await _t.SessionService().CompleteAsync(monday.SessionId, TestData.Completion(score: 9));
        var friday = (await _t.SessionService().SearchAsync(new TrainingSessionQueryDto { From = Today.AddDays(4), To = Today.AddDays(4) })).Data!.Single();
        await _t.SessionService().CancelAsync(friday.SessionId, new CancelTrainingSessionRequestDto { Reason = "Mưa" });

        var result = await _t.SessionService().GetHorseWeeklyScheduleAsync(_horseId, Today.AddDays(5));

        Assert.True(result.Success);
        var week = result.Data!;
        Assert.Equal("Thunder", week.HorseName);
        Assert.Equal(Today, week.WeekStartDate);
        Assert.Equal(Today.AddDays(6), week.WeekEndDate);
        Assert.Equal(7, week.Days.Count);
        Assert.Equal(["Thứ Hai", "Thứ Ba", "Thứ Tư", "Thứ Năm", "Thứ Sáu", "Thứ Bảy", "Chủ Nhật"], week.Days.Select(d => d.DayOfWeek).ToArray());
        Assert.Equal([false, false, true, false, true, true, true], week.Days.Select(d => d.IsRestDay).ToArray());
        Assert.Single(week.Days[4].Sessions);
        Assert.Equal(3, week.Summary.TotalSessions);
        Assert.Equal(1, week.Summary.CompletedSessions);
        Assert.Equal(1, week.Summary.CancelledSessions);
        Assert.Equal(9m, week.Summary.PlannedDistanceKm);
        Assert.Equal(3.2m, week.Summary.ActualDistanceKm);
        Assert.Equal(105, week.Summary.PlannedDurationMinutes);
        Assert.Equal(9m, week.Summary.AveragePerformanceScore);
    }

    [Fact]
    public async Task WeeklySchedule_DefaultsToCurrentWeek_AndUnknownHorseIsNotFound()
    {
        _t.Clock.Advance(TimeSpan.FromDays(3));

        var result = await _t.SessionService().GetHorseWeeklyScheduleAsync(_horseId, null);
        var missing = await _t.SessionService().GetHorseWeeklyScheduleAsync(9999, null);

        Assert.Equal(Today, result.Data!.WeekStartDate);
        Assert.All(result.Data.Days, d => Assert.True(d.IsRestDay));
        Assert.Null(result.Data.Summary.AveragePerformanceScore);
        Assert.Equal(ServiceErrorCodes.NotFound, missing.ErrorCode);
    }
}
