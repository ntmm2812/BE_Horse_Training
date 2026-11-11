using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SWP.Domain.Common;
using SWP.Tests.Support;

namespace SWP.Tests.Api;

public class GroomSessionReportApiTests : IClassFixture<TrainingApiFactory>
{
    private readonly TrainingApiFactory f;
    public GroomSessionReportApiTests(TrainingApiFactory factory) => f = factory;

    private static async Task<JsonElement> Read(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == expected, $"{response.StatusCode}: {body}");
        return string.IsNullOrEmpty(body) ? default : JsonSerializer.Deserialize<JsonElement>(body);
    }

    // Buổi tập bắt đầu 06:00 của ngày `date`; đồng hồ test là 08:00 ngày TestData.Today.
    private int NewSession(DateOnly date, int? assignedTo, string status = TrainingSessionStatuses.Scheduled) => f.WithDb(db =>
    {
        var horseId = TestData.AddHorse(db, f.Ids.OwnerId, $"Report horse {Guid.NewGuid():N}");
        var (planId, phaseId) = TestData.AddPlan(db, horseId, TestData.Today, TestData.Today.AddDays(7));
        return TestData.AddSession(db, planId, date, "Trot", status, phaseId: phaseId, assignedToUserId: assignedTo);
    });

    private static object Body(string result = "Completed", decimal? km = 3.2m, string reaction = "Good", string? note = "Ngựa chạy ổn định") =>
        new { result, actualDistanceKm = km, horseReaction = reaction, note };

    private Task<HttpResponseMessage> Report(HttpClient client, int id, object body) =>
        client.PostAsJsonAsync($"/api/groom/training-sessions/{id}/report", body);

    [Fact]
    public async Task AssignedGroom_ReportsStartedSession_AndTrainerSeesReportInApi()
    {
        var id = NewSession(TestData.Today, f.Ids.GroomId);

        var report = await Read(await Report(f.Groom(), id, Body()));
        Assert.Equal("Completed", report.GetProperty("result").GetString());
        Assert.Equal(3.2m, report.GetProperty("actualDistanceKm").GetDecimal());
        Assert.Equal("Good", report.GetProperty("horseReaction").GetString());
        Assert.Equal("Ngựa chạy ổn định", report.GetProperty("note").GetString());
        Assert.False(string.IsNullOrEmpty(report.GetProperty("reportedByName").GetString()));

        var range = $"from={TestData.Today:yyyy-MM-dd}&to={TestData.Today:yyyy-MM-dd}";
        var trainerRows = await Read(await f.Trainer().GetAsync($"/api/trainer/training-sessions?{range}"));
        var trainerRow = trainerRows.EnumerateArray().Single(s => s.GetProperty("sessionId").GetInt32() == id);
        Assert.Equal("Completed", trainerRow.GetProperty("report").GetProperty("result").GetString());
        Assert.Equal("Scheduled", trainerRow.GetProperty("status").GetString());

        var groomRows = await Read(await f.Groom().GetAsync($"/api/training-assignments/me?{range}"));
        Assert.Equal("Good", groomRows.EnumerateArray().Single(s => s.GetProperty("sessionId").GetInt32() == id)
            .GetProperty("report").GetProperty("horseReaction").GetString());
    }

    [Fact]
    public async Task SessionWithoutReport_HasNullReport_AndOtherFieldsUnchanged()
    {
        var id = NewSession(TestData.Today, f.Ids.GroomId);
        var rows = await Read(await f.Trainer().GetAsync($"/api/trainer/training-sessions?from={TestData.Today:yyyy-MM-dd}&to={TestData.Today:yyyy-MM-dd}"));
        var row = rows.EnumerateArray().Single(s => s.GetProperty("sessionId").GetInt32() == id);
        Assert.Equal(JsonValueKind.Null, row.GetProperty("report").ValueKind);
        Assert.Equal("Trot", row.GetProperty("trainingType").GetString());
    }

    [Fact]
    public async Task SecondReport_ForSameSession_IsRejectedWithConflict()
    {
        var id = NewSession(TestData.Today, f.Ids.GroomId);
        await Read(await Report(f.Groom(), id, Body()));
        var second = await Read(await Report(f.Groom(), id, Body("Abandoned")), HttpStatusCode.Conflict);
        Assert.Equal("Buổi tập này đã có báo cáo.", second.GetProperty("message").GetString());
    }

    [Fact]
    public async Task SessionNotYetStarted_CannotBeReported()
    {
        var id = NewSession(TestData.Today.AddDays(1), f.Ids.GroomId);
        var error = await Read(await Report(f.Groom(), id, Body()), HttpStatusCode.Conflict);
        Assert.Equal("INVALID_STATUS", error.GetProperty("code").GetString());
    }

    [Fact]
    public async Task OnlyTheAssignedGroom_CanReport()
    {
        var unassigned = NewSession(TestData.Today, null);
        var other = NewSession(TestData.Today, f.Ids.PendingGroomId);
        await Read(await Report(f.Groom(), unassigned, Body()), HttpStatusCode.Conflict);
        await Read(await Report(f.Groom(), other, Body()), HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CancelledSession_CannotBeReported()
    {
        var id = NewSession(TestData.Today, f.Ids.GroomId, TrainingSessionStatuses.Cancelled);
        var error = await Read(await Report(f.Groom(), id, Body()), HttpStatusCode.Conflict);
        Assert.Equal("INVALID_STATUS", error.GetProperty("code").GetString());
    }

    [Fact]
    public async Task UnknownSession_ReturnsNotFound()
    {
        await Read(await Report(f.Groom(), 999999, Body()), HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("Trainer")]
    [InlineData("Owner")]
    [InlineData("Anonymous")]
    public async Task OtherRoles_CannotReport(string who)
    {
        var id = NewSession(TestData.Today, f.Ids.GroomId);
        var client = who switch
        {
            "Trainer" => f.Trainer(),
            "Owner" => f.ClientAs(f.Ids.OwnerId, "Horse Owner"),
            _ => f.Anonymous(),
        };
        var response = await Report(client, id, Body());
        Assert.Equal(who == "Anonymous" ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("", 3, "Good", "ok")]
    [InlineData("Bogus", 3, "Good", "ok")]
    [InlineData("Completed", 3, "", "ok")]
    [InlineData("Completed", 3, "Bogus", "ok")]
    [InlineData("Completed", 51, "Good", "ok")]
    [InlineData("Completed", -1, "Good", "ok")]
    [InlineData("NotTrained", 2, "Normal", "ok")]
    public async Task InvalidBody_ReturnsBadRequest_AndSavesNothing(string result, double km, string reaction, string note)
    {
        var id = NewSession(TestData.Today, f.Ids.GroomId);
        await Read(await Report(f.Groom(), id, Body(result, (decimal)km, reaction, note)), HttpStatusCode.BadRequest);
        await Read(await Report(f.Groom(), id, Body()));
    }

    [Fact]
    public async Task NoteOver300Chars_IsRejected_AndNotTrainedWithoutKmIsAccepted()
    {
        var id = NewSession(TestData.Today, f.Ids.GroomId);
        await Read(await Report(f.Groom(), id, Body(note: new string('a', 301))), HttpStatusCode.BadRequest);
        var report = await Read(await Report(f.Groom(), id, Body("NotTrained", null, "Normal", null)));
        Assert.Equal(JsonValueKind.Null, report.GetProperty("actualDistanceKm").ValueKind);
        Assert.Equal("", report.GetProperty("note").GetString());
    }
}
