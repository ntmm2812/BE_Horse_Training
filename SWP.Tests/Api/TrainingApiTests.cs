using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SWP.Application.DTOs.Training;
using SWP.Application.Training;
using SWP.Domain.Common;
using SWP.Tests.Support;

namespace SWP.Tests.Api;

public class TrainingApiTests : IClassFixture<TrainingApiFactory>
{
    private static readonly JsonSerializerOptions Json = TrainingApiFactory.Json;

    private readonly TrainingApiFactory _factory;

    public TrainingApiTests(TrainingApiFactory factory)
    {
        _factory = factory;
    }

    private static DateOnly Today => TestData.Today;

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, HttpStatusCode expected)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == expected, $"Expected {expected} but got {response.StatusCode}: {body}");
        return JsonSerializer.Deserialize<T>(body, Json)!;
    }

    private sealed record ErrorBody(string Message, string Code, string[] Details);

    // ---------- Phân quyền ----------

    [Theory]
    [InlineData("GET", "/api/trainer/training-plans")]
    [InlineData("GET", "/api/trainer/training-plans/options")]
    [InlineData("GET", "/api/trainer/horses/1/fitness-assessment")]
    [InlineData("GET", "/api/trainer/horses/1/weekly-schedule")]
    [InlineData("GET", "/api/trainer/training-sessions")]
    [InlineData("POST", "/api/trainer/training-plans/1/activate")]
    [InlineData("POST", "/api/trainer/training-sessions/1/start")]
    [InlineData("GET", "/api/training-assignments/me")]
    public async Task Endpoints_RequireAuthentication(string method, string url)
    {
        var response = await _factory.Anonymous().SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("Horse Owner")]
    [InlineData("Groom")]
    [InlineData("Veterinarian")]
    [InlineData("Club Manager")]
    public async Task TrainerEndpoints_AreForHeadTrainerOnly(string role)
    {
        var client = _factory.ClientAs(_factory.Ids.OwnerId, role);

        var plans = await client.GetAsync("/api/trainer/training-plans");
        var create = await client.PostAsJsonAsync("/api/trainer/training-plans", new CreateTrainingPlanRequestDto(), Json);

        Assert.Equal(HttpStatusCode.Forbidden, plans.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
    }

    [Fact]
    public async Task Assignments_AreForGroomAndTrainer()
    {
        var groom = await _factory.Groom().GetAsync("/api/training-assignments/me");
        var trainer = await _factory.Trainer().GetAsync("/api/training-assignments/me");
        var owner = await _factory.ClientAs(_factory.Ids.OwnerId, "Horse Owner").GetAsync("/api/training-assignments/me");

        Assert.Equal(HttpStatusCode.OK, groom.StatusCode);
        Assert.Equal(HttpStatusCode.OK, trainer.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, owner.StatusCode);
    }

    // ---------- Luồng chính ----------

    [Fact]
    public async Task FullFlow_FromFitnessAssessmentToCompletedPlan()
    {
        var trainer = _factory.Trainer();
        var horseId = _factory.AddHorse("Api Thunder");

        // 1. Xem thể trạng
        var fitness = await ReadAsync<HorseFitnessAssessmentDto>(
            await trainer.GetAsync($"/api/trainer/horses/{horseId}/fitness-assessment"), HttpStatusCode.OK);
        Assert.Equal(FitnessReadiness.Ready, fitness.Readiness);

        // 2. Gợi ý giáo án theo thể trạng
        var suggestion = await ReadAsync<TrainingPlanSuggestionDto>(
            await trainer.GetAsync($"/api/trainer/horses/{horseId}/plan-suggestion?weeks=6"), HttpStatusCode.OK);
        Assert.True(suggestion.CanCreatePlan);
        Assert.Equal(Today, suggestion.StartDate);

        // 3. Lập giáo án (Nháp)
        var created = await ReadAsync<TrainingPlanDetailDto>(
            await trainer.PostAsJsonAsync("/api/trainer/training-plans", new CreateTrainingPlanRequestDto
            {
                HorseId = horseId,
                PlanName = suggestion.SuggestedPlanName!,
                TrainingType = suggestion.SuggestedTrainingType,
                Objective = "Chuẩn bị giải mùa đông",
                Phases = suggestion.Phases.ToList(),
            }, Json),
            HttpStatusCode.Created);
        Assert.Equal(TrainingPlanStatuses.Draft, created.Status);
        Assert.Equal(_factory.Ids.TrainerId, created.CreatedByUserId);
        var planId = created.PlanId;

        // 4. Xem trước lịch tuần khi còn Nháp
        var preview = await ReadAsync<WeeklyScheduleGenerationResultDto>(
            await trainer.PostAsJsonAsync($"/api/trainer/training-plans/{planId}/weekly-schedule",
                new GenerateWeeklyScheduleRequestDto { WeekStartDate = Today, PreviewOnly = true }, Json),
            HttpStatusCode.OK);
        Assert.True(preview.PreviewOnly);
        Assert.NotEmpty(preview.Sessions);

        // 5. Áp dụng giáo án
        var active = await ReadAsync<TrainingPlanDetailDto>(
            await trainer.PostAsync($"/api/trainer/training-plans/{planId}/activate", null), HttpStatusCode.OK);
        Assert.Equal(TrainingPlanStatuses.Active, active.Status);

        // 6. Xếp lịch tuần và phân công cho nhân viên chăm sóc
        var generated = await ReadAsync<WeeklyScheduleGenerationResultDto>(
            await trainer.PostAsJsonAsync($"/api/trainer/training-plans/{planId}/weekly-schedule",
                new GenerateWeeklyScheduleRequestDto { WeekStartDate = Today, AssignedToUserId = _factory.Ids.GroomId }, Json),
            HttpStatusCode.Created);
        Assert.Equal(preview.Sessions.Count, generated.CreatedCount);

        // 7. Nhân viên chăm sóc thấy lịch được giao
        var mine = await ReadAsync<List<TrainingSessionDto>>(
            await _factory.Groom().GetAsync($"/api/training-assignments/me?from={Today:yyyy-MM-dd}&to={Today.AddDays(6):yyyy-MM-dd}"),
            HttpStatusCode.OK);
        Assert.Equal(
            generated.Sessions.Select(s => s.SessionId).Order(),
            mine.Where(s => s.PlanId == planId).Select(s => s.SessionId).Order());

        // 8. Thêm thủ công một buổi chiều nay
        var extra = await ReadAsync<TrainingSessionDto>(
            await trainer.PostAsJsonAsync($"/api/trainer/training-plans/{planId}/sessions",
                TestData.Session(Today, TrainingTypes.Walk, 2m, 25, startTime: "16:30"), Json),
            HttpStatusCode.Created);
        Assert.Equal("16:30", extra.StartTime);

        // 9. Thực hiện buổi sáng: phân công lại → bắt đầu → ghi kết quả
        var todaySession = generated.Sessions.Single(s => s.TrainingDate == Today);
        var reassigned = await ReadAsync<TrainingSessionDto>(
            await trainer.PutAsJsonAsync($"/api/trainer/training-sessions/{todaySession.SessionId}/assignment",
                new AssignTrainingSessionRequestDto { AssignedToUserId = _factory.Ids.TrainerId }, Json),
            HttpStatusCode.OK);
        Assert.Equal(_factory.Ids.TrainerId, reassigned.AssignedToUserId);

        var started = await ReadAsync<TrainingSessionDto>(
            await trainer.PostAsync($"/api/trainer/training-sessions/{todaySession.SessionId}/start", null), HttpStatusCode.OK);
        Assert.Equal(TrainingSessionStatuses.InProgress, started.Status);

        var completed = await ReadAsync<TrainingSessionDto>(
            await trainer.PostAsJsonAsync($"/api/trainer/training-sessions/{todaySession.SessionId}/complete",
                TestData.Completion(score: 8), Json),
            HttpStatusCode.OK);
        Assert.Equal(TrainingSessionStatuses.Completed, completed.Status);
        Assert.Equal(8m, completed.PerformanceScore);
        Assert.Equal("Ngựa giữ nhịp tốt, hồi sức nhanh.", completed.TrainerComment);

        // 10. Lịch tuần của ngựa phản ánh kết quả
        var week = await ReadAsync<HorseWeeklyScheduleDto>(
            await trainer.GetAsync($"/api/trainer/horses/{horseId}/weekly-schedule?weekStart={Today:yyyy-MM-dd}"),
            HttpStatusCode.OK);
        Assert.Equal(7, week.Days.Count);
        Assert.Equal(2, week.Days[0].Sessions.Count);
        Assert.Equal(1, week.Summary.CompletedSessions);
        Assert.Equal(8m, week.Summary.AveragePerformanceScore);

        // 11. Kết thúc giáo án → các buổi còn lại bị hủy
        var closed = await ReadAsync<TrainingPlanDetailDto>(
            await trainer.PostAsync($"/api/trainer/training-plans/{planId}/complete", null), HttpStatusCode.OK);
        Assert.Equal(TrainingPlanStatuses.Completed, closed.Status);
        Assert.Equal(1, closed.Progress.CompletedSessions);
        Assert.Equal(0, closed.Progress.UpcomingSessions);
        Assert.All(
            closed.Sessions.Where(s => s.SessionId != todaySession.SessionId),
            s => Assert.Equal(TrainingSessionStatuses.Cancelled, s.Status));
    }

    [Fact]
    public async Task SecondaryEndpoints_WorkOverHttp()
    {
        var trainer = _factory.Trainer();
        var horseId = _factory.AddHorse("Api Secondary");
        var draft = await ReadAsync<TrainingPlanDetailDto>(
            await trainer.PostAsJsonAsync("/api/trainer/training-plans", TestData.ThreePhasePlan(horseId), Json),
            HttpStatusCode.Created);

        // Sửa giáo án Nháp (kể cả giai đoạn) rồi áp dụng
        var updated = await ReadAsync<TrainingPlanDetailDto>(
            await trainer.PutAsJsonAsync($"/api/trainer/training-plans/{draft.PlanId}", new UpdateTrainingPlanRequestDto
            {
                PlanName = "Giáo án đã sửa",
                Phases = [TestData.Phase("Duy nhất", Today, Today.AddDays(13), TrainingIntensityLevels.Medium)],
            }, Json),
            HttpStatusCode.OK);
        Assert.Equal("Giáo án đã sửa", updated.PlanName);
        Assert.Single(updated.Phases);
        await trainer.PostAsync($"/api/trainer/training-plans/{draft.PlanId}/activate", null);

        // Tìm giáo án theo ngựa + trạng thái
        var plans = await ReadAsync<List<TrainingPlanDto>>(
            await trainer.GetAsync($"/api/trainer/training-plans?horseId={horseId}&status=Active"), HttpStatusCode.OK);
        Assert.Equal(draft.PlanId, Assert.Single(plans).PlanId);

        // Tạo, sửa, tìm, hủy buổi tập
        var session = await ReadAsync<TrainingSessionDto>(
            await trainer.PostAsJsonAsync($"/api/trainer/training-plans/{draft.PlanId}/sessions",
                TestData.Session(Today.AddDays(2), TrainingTypes.Trot), Json),
            HttpStatusCode.Created);
        var edited = await ReadAsync<TrainingSessionDto>(
            await trainer.PutAsJsonAsync($"/api/trainer/training-sessions/{session.SessionId}",
                TestData.Session(Today.AddDays(3), TrainingTypes.Canter, 3.5m, 45, TrackSurfaces.Synthetic), Json),
            HttpStatusCode.OK);
        Assert.Equal(TrainingTypes.Canter, edited.TrainingType);
        Assert.Equal(TrackSurfaces.Synthetic, edited.Surface);

        var found = await ReadAsync<List<TrainingSessionDto>>(
            await trainer.GetAsync($"/api/trainer/training-sessions?horseId={horseId}&from={Today:yyyy-MM-dd}&to={Today.AddDays(6):yyyy-MM-dd}&status=scheduled"),
            HttpStatusCode.OK);
        Assert.Equal(session.SessionId, Assert.Single(found).SessionId);

        var cancelled = await ReadAsync<TrainingSessionDto>(
            await trainer.PostAsJsonAsync($"/api/trainer/training-sessions/{session.SessionId}/cancel",
                new CancelTrainingSessionRequestDto { Reason = "Sân ướt" }, Json),
            HttpStatusCode.OK);
        Assert.Equal("Đã hủy: Sân ướt", cancelled.Result);

        // Hủy giáo án
        var closed = await ReadAsync<TrainingPlanDetailDto>(
            await trainer.PostAsync($"/api/trainer/training-plans/{draft.PlanId}/cancel", null), HttpStatusCode.OK);
        Assert.Equal(TrainingPlanStatuses.Cancelled, closed.Status);
    }

    // ---------- Ánh xạ lỗi ----------

    [Fact]
    public async Task UnknownResources_Return404WithCode()
    {
        var trainer = _factory.Trainer();

        var plan = await trainer.GetAsync("/api/trainer/training-plans/999999");
        var session = await trainer.PostAsync("/api/trainer/training-sessions/999999/start", null);
        var fitness = await trainer.GetAsync("/api/trainer/horses/999999/fitness-assessment");

        Assert.Equal("NOT_FOUND", (await ReadAsync<ErrorBody>(plan, HttpStatusCode.NotFound)).Code);
        Assert.Equal("NOT_FOUND", (await ReadAsync<ErrorBody>(session, HttpStatusCode.NotFound)).Code);
        Assert.Equal("NOT_FOUND", (await ReadAsync<ErrorBody>(fitness, HttpStatusCode.NotFound)).Code);
    }

    [Fact]
    public async Task InvalidPlan_Returns400()
    {
        var horseId = _factory.AddHorse("Api Invalid");

        var response = await _factory.Trainer().PostAsJsonAsync("/api/trainer/training-plans",
            new CreateTrainingPlanRequestDto { HorseId = horseId, PlanName = "Thiếu giai đoạn" }, Json);

        var error = await ReadAsync<ErrorBody>(response, HttpStatusCode.BadRequest);
        Assert.Equal("VALIDATION_ERROR", error.Code);
        Assert.Contains("giai đoạn", error.Message);
    }

    [Fact]
    public async Task UnfitHorse_Returns422WithFitnessWarnings()
    {
        var horseId = _factory.AddHorse("Api Quarantine", HorseHealthStatuses.CachLy);

        var response = await _factory.Trainer().PostAsJsonAsync("/api/trainer/training-plans",
            TestData.ThreePhasePlan(horseId), Json);

        var error = await ReadAsync<ErrorBody>(response, HttpStatusCode.UnprocessableEntity);
        Assert.Equal("HORSE_NOT_FIT", error.Code);
        Assert.Contains(error.Details, d => d.Contains("cách ly"));
    }

    [Fact]
    public async Task InvalidStateTransition_Returns409()
    {
        var trainer = _factory.Trainer();
        var horseId = _factory.AddHorse("Api Conflict");
        var plan = await ReadAsync<TrainingPlanDetailDto>(
            await trainer.PostAsJsonAsync("/api/trainer/training-plans", TestData.ThreePhasePlan(horseId), Json),
            HttpStatusCode.Created);
        await trainer.PostAsync($"/api/trainer/training-plans/{plan.PlanId}/activate", null);

        var again = await trainer.PostAsync($"/api/trainer/training-plans/{plan.PlanId}/activate", null);
        var delete = await trainer.DeleteAsync($"/api/trainer/training-plans/{plan.PlanId}");

        Assert.Equal("INVALID_STATUS", (await ReadAsync<ErrorBody>(again, HttpStatusCode.Conflict)).Code);
        Assert.Equal("INVALID_STATUS", (await ReadAsync<ErrorBody>(delete, HttpStatusCode.Conflict)).Code);
    }

    [Fact]
    public async Task Deletes_Return204()
    {
        var trainer = _factory.Trainer();
        var draft = await ReadAsync<TrainingPlanDetailDto>(
            await trainer.PostAsJsonAsync("/api/trainer/training-plans", TestData.ThreePhasePlan(_factory.AddHorse("Api Draft")), Json),
            HttpStatusCode.Created);
        var activeHorse = _factory.AddHorse("Api Delete Session");
        var active = await ReadAsync<TrainingPlanDetailDto>(
            await trainer.PostAsJsonAsync("/api/trainer/training-plans", TestData.ThreePhasePlan(activeHorse), Json),
            HttpStatusCode.Created);
        await trainer.PostAsync($"/api/trainer/training-plans/{active.PlanId}/activate", null);
        var session = await ReadAsync<TrainingSessionDto>(
            await trainer.PostAsJsonAsync($"/api/trainer/training-plans/{active.PlanId}/sessions",
                TestData.Session(Today.AddDays(1), TrainingTypes.Trot), Json),
            HttpStatusCode.Created);

        var deletePlan = await trainer.DeleteAsync($"/api/trainer/training-plans/{draft.PlanId}");
        var deleteSession = await trainer.DeleteAsync($"/api/trainer/training-sessions/{session.SessionId}");

        Assert.Equal(HttpStatusCode.NoContent, deletePlan.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deleteSession.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await trainer.GetAsync($"/api/trainer/training-sessions/{session.SessionId}")).StatusCode);
    }

    [Fact]
    public async Task MalformedQueryDate_Returns400()
    {
        var response = await _factory.Trainer().GetAsync("/api/trainer/horses/1/weekly-schedule?weekStart=not-a-date");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- Tiện ích cho FE ----------

    [Fact]
    public async Task Options_AndStaff_AreAvailable()
    {
        var trainer = _factory.Trainer();

        var options = await ReadAsync<TrainingOptionsDto>(await trainer.GetAsync("/api/trainer/training-plans/options"), HttpStatusCode.OK);
        var staff = await ReadAsync<List<TrainingStaffDto>>(await trainer.GetAsync("/api/trainer/training-sessions/assignable-staff"), HttpStatusCode.OK);

        Assert.Contains(options.TrainingTypes, t => t.Code == TrainingTypes.Gallop && t.Label == "Phi nước đại");
        Assert.Contains(options.Surfaces, s => s.Code == TrackSurfaces.Turf);
        Assert.Contains(staff, s => s.UserId == _factory.Ids.GroomId && s.RoleId == RoleNames.Groom);
        Assert.DoesNotContain(staff, s => s.UserId == _factory.Ids.PendingGroomId);
    }

    [Fact]
    public async Task SwaggerDocument_IncludesTrainingEndpoints()
    {
        var response = await _factory.Anonymous().GetAsync("/swagger/v1/swagger.json");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("/api/trainer/training-plans/{planId}/weekly-schedule", body);
        Assert.Contains("/api/trainer/training-sessions/{sessionId}/complete", body);
        Assert.Contains("/api/trainer/horses/{horseId}/fitness-assessment", body);
    }
}
