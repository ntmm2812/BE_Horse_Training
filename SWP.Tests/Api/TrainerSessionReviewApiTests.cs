using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SWP.Domain.Common;
using SWP.Domain.Models.Entity;
using SWP.Tests.Support;

namespace SWP.Tests.Api;

public class TrainerSessionReviewApiTests : IClassFixture<TrainingApiFactory>
{
    private readonly TrainingApiFactory f;
    public TrainerSessionReviewApiTests(TrainingApiFactory factory) => f = factory;

    private static async Task<JsonElement> Read(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == expected, $"{response.StatusCode}: {body}");
        return string.IsNullOrEmpty(body) ? default : JsonSerializer.Deserialize<JsonElement>(body);
    }

    private HttpClient Owner() => f.ClientAs(f.Ids.OwnerId, "Horse Owner");

    // Buổi tập hôm nay (đồng hồ test 08:00) của ngựa thuộc `ownerId`; withReport: Groom đã báo cáo với quãng đường `km`.
    private (int SessionId, int HorseId) NewSession(
        bool withReport = true,
        decimal? km = 3.2m,
        string result = "Completed",
        string status = TrainingSessionStatuses.Scheduled,
        int? ownerId = null) => f.WithDb(db =>
    {
        var horseId = TestData.AddHorse(db, ownerId ?? f.Ids.OwnerId, $"Review horse {Guid.NewGuid():N}");
        var (planId, phaseId) = TestData.AddPlan(db, horseId, TestData.Today, TestData.Today.AddDays(7));
        var id = TestData.AddSession(db, planId, TestData.Today, "Trot", status, phaseId: phaseId, assignedToUserId: f.Ids.GroomId);
        if (withReport)
        {
            db.TrainingSessionReports.Add(new TrainingSessionReport
            {
                ScheduleId = id,
                Result = result,
                ActualDistanceKm = km,
                HorseReaction = "Good",
                Note = "",
                ReportedByUserId = f.Ids.GroomId,
                ReportedAt = TestData.Now.UtcDateTime,
            });
            db.SaveChanges();
        }

        return (id, horseId);
    });

    private int NewOtherOwner() => f.WithDb(db =>
    {
        var role = db.Roles.Single(r => r.RoleName == "Horse Owner");
        var user = new User
        {
            FullName = "Chủ ngựa khác",
            Email = $"other-{Guid.NewGuid():N}@test.vn",
            Status = UserStatuses.Active,
            RoleId = role.RoleId,
            CreatedAt = TestData.Now.UtcDateTime,
        };
        db.Users.Add(user);
        db.SaveChanges();
        return user.UserId;
    });

    private static object Body(int? score = 8, string assessment = "Progress", string? comment = "Giữ nhịp tốt ở đoạn cuối.") =>
        new { score, assessment, comment };

    private Task<HttpResponseMessage> Review(HttpClient client, int id, object body) =>
        client.PostAsJsonAsync($"/api/trainer/training-sessions/{id}/review", body);

    private Task<HttpResponseMessage> EditReview(HttpClient client, int id, object body) =>
        client.PutAsJsonAsync($"/api/trainer/training-sessions/{id}/review", body);

    private Task<HttpResponseMessage> Send(HttpClient client, int id) =>
        client.PostAsync($"/api/trainer/training-sessions/{id}/review/send-to-owner", null);

    private async Task<JsonElement> TrainerRow(int id)
    {
        var range = $"from={TestData.Today:yyyy-MM-dd}&to={TestData.Today:yyyy-MM-dd}";
        var rows = await Read(await f.Trainer().GetAsync($"/api/trainer/training-sessions?{range}"));
        return rows.EnumerateArray().Single(s => s.GetProperty("sessionId").GetInt32() == id);
    }

    private async Task<JsonElement> OwnerReviews(HttpClient client, string query = "")
        => await Read(await client.GetAsync($"/api/owner/session-reviews{query}"));

    [Fact]
    public async Task Trainer_ReviewsReportedSession_AndSeesReviewInApi_WithStatusUnchanged()
    {
        var (id, _) = NewSession();

        var review = await Read(await Review(f.Trainer(), id, Body()));
        Assert.Equal(8, review.GetProperty("score").GetInt32());
        Assert.Equal("Progress", review.GetProperty("assessment").GetString());
        Assert.Equal("Giữ nhịp tốt ở đoạn cuối.", review.GetProperty("comment").GetString());
        Assert.False(string.IsNullOrEmpty(review.GetProperty("reviewedByName").GetString()));
        Assert.Equal(JsonValueKind.Null, review.GetProperty("sentToOwnerAt").ValueKind);

        var row = await TrainerRow(id);
        Assert.Equal(8, row.GetProperty("review").GetProperty("score").GetInt32());
        Assert.Equal("Scheduled", row.GetProperty("status").GetString());
        Assert.Equal("Completed", row.GetProperty("report").GetProperty("result").GetString());
    }

    [Fact]
    public async Task SessionWithoutReview_HasNullReview()
    {
        var (id, _) = NewSession();
        Assert.Equal(JsonValueKind.Null, (await TrainerRow(id)).GetProperty("review").ValueKind);
    }

    [Fact]
    public async Task SecondReview_IsRejectedWithConflict()
    {
        var (id, _) = NewSession();
        await Read(await Review(f.Trainer(), id, Body()));
        var second = await Read(await Review(f.Trainer(), id, Body(5)), HttpStatusCode.Conflict);
        Assert.Equal("CONFLICT", second.GetProperty("code").GetString());
        Assert.Equal(8, (await TrainerRow(id)).GetProperty("review").GetProperty("score").GetInt32());
    }

    [Fact]
    public async Task SessionWithoutReport_CannotBeReviewed()
    {
        var (id, _) = NewSession(withReport: false);
        var error = await Read(await Review(f.Trainer(), id, Body()), HttpStatusCode.Conflict);
        Assert.Equal("INVALID_STATUS", error.GetProperty("code").GetString());
    }

    [Theory]
    [InlineData("Completed", 0.0)]
    [InlineData("NotTrained", null)]
    [InlineData("Abandoned", 0.0)]
    public async Task ReportWithoutDistance_CanStillBeReviewed(string result, double? km)
    {
        var (id, _) = NewSession(km: km is null ? null : (decimal)km, result: result);
        var review = await Read(await Review(f.Trainer(), id, Body()));
        Assert.Equal(8, review.GetProperty("score").GetInt32());
        await Read(await Send(f.Trainer(), id));
    }

    [Fact]
    public async Task CancelledSession_CannotBeReviewed()
    {
        var (id, _) = NewSession(status: TrainingSessionStatuses.Cancelled);
        await Read(await Review(f.Trainer(), id, Body()), HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UnknownSession_ReturnsNotFound()
    {
        await Read(await Review(f.Trainer(), 999999, Body()), HttpStatusCode.NotFound);
        await Read(await EditReview(f.Trainer(), 999999, Body()), HttpStatusCode.NotFound);
        await Read(await Send(f.Trainer(), 999999), HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(0, "Progress", "ok")]
    [InlineData(11, "Progress", "ok")]
    [InlineData(null, "Progress", "ok")]
    [InlineData(8, "", "ok")]
    [InlineData(8, "Bogus", "ok")]
    [InlineData(8, "Progress", "")]
    [InlineData(8, "Progress", "   ")]
    public async Task InvalidBody_ReturnsBadRequest_AndSavesNothing(int? score, string assessment, string comment)
    {
        var (id, _) = NewSession();
        await Read(await Review(f.Trainer(), id, Body(score, assessment, comment)), HttpStatusCode.BadRequest);
        Assert.Equal(JsonValueKind.Null, (await TrainerRow(id)).GetProperty("review").ValueKind);
        await Read(await Review(f.Trainer(), id, Body()));
    }

    [Fact]
    public async Task CommentOver500Chars_IsRejected()
    {
        var (id, _) = NewSession();
        await Read(await Review(f.Trainer(), id, Body(comment: new string('a', 501))), HttpStatusCode.BadRequest);
        await Read(await Review(f.Trainer(), id, Body(comment: new string('a', 500))));
    }

    [Fact]
    public async Task Update_EditsUnsentReview_ButNotWithoutOneOrAfterSending()
    {
        var (id, _) = NewSession();
        await Read(await EditReview(f.Trainer(), id, Body()), HttpStatusCode.NotFound);

        await Read(await Review(f.Trainer(), id, Body()));
        var edited = await Read(await EditReview(f.Trainer(), id, Body(5, "Attention", "Cần theo dõi chân trước.")));
        Assert.Equal(5, edited.GetProperty("score").GetInt32());
        Assert.Equal("Attention", edited.GetProperty("assessment").GetString());
        Assert.Equal("Cần theo dõi chân trước.", (await TrainerRow(id)).GetProperty("review").GetProperty("comment").GetString());

        await Read(await EditReview(f.Trainer(), id, Body(0)), HttpStatusCode.BadRequest);

        await Read(await Send(f.Trainer(), id));
        var locked = await Read(await EditReview(f.Trainer(), id, Body(9)), HttpStatusCode.Conflict);
        Assert.Equal("CONFLICT", locked.GetProperty("code").GetString());
        Assert.Equal(5, (await TrainerRow(id)).GetProperty("review").GetProperty("score").GetInt32());
    }

    [Fact]
    public async Task Send_RequiresReview_AndCanOnlyHappenOnce()
    {
        var (id, _) = NewSession();
        var none = await Read(await Send(f.Trainer(), id), HttpStatusCode.Conflict);
        Assert.Equal("INVALID_STATUS", none.GetProperty("code").GetString());

        await Read(await Review(f.Trainer(), id, Body()));
        var sent = await Read(await Send(f.Trainer(), id));
        Assert.Equal(JsonValueKind.String, sent.GetProperty("sentToOwnerAt").ValueKind);
        Assert.Equal(JsonValueKind.String, (await TrainerRow(id)).GetProperty("review").GetProperty("sentToOwnerAt").ValueKind);

        await Read(await Send(f.Trainer(), id), HttpStatusCode.Conflict);
        await Read(await Review(f.Trainer(), id, Body()), HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Owner_SeesOnlySentReviews_WithoutGroomReport()
    {
        var (id, horseId) = NewSession();
        await Read(await Review(f.Trainer(), id, Body()));

        Assert.Empty((await OwnerReviews(Owner(), $"?horseId={horseId}")).EnumerateArray().ToList());

        await Read(await Send(f.Trainer(), id));
        var list = await OwnerReviews(Owner(), $"?horseId={horseId}");
        var item = list.EnumerateArray().Single();
        Assert.Equal(id, item.GetProperty("sessionId").GetInt32());
        Assert.Equal(horseId, item.GetProperty("horseId").GetInt32());
        Assert.StartsWith("Review horse", item.GetProperty("horseName").GetString());
        Assert.Equal(TestData.Today.ToString("yyyy-MM-dd"), item.GetProperty("trainingDate").GetString());
        Assert.Equal(8, item.GetProperty("score").GetInt32());
        Assert.Equal("Progress", item.GetProperty("assessment").GetString());
        Assert.Equal("Giữ nhịp tốt ở đoạn cuối.", item.GetProperty("comment").GetString());
        Assert.False(string.IsNullOrEmpty(item.GetProperty("trainerName").GetString()));
        Assert.False(item.TryGetProperty("report", out _));
    }

    [Fact]
    public async Task Owner_DoesNotSeeOtherOwnersHorses()
    {
        var (id, horseId) = NewSession();
        await Read(await Review(f.Trainer(), id, Body()));
        await Read(await Send(f.Trainer(), id));

        var otherOwner = f.ClientAs(NewOtherOwner(), "Horse Owner");
        Assert.DoesNotContain(
            (await OwnerReviews(otherOwner)).EnumerateArray().ToList(),
            r => r.GetProperty("sessionId").GetInt32() == id);
        await Read(await otherOwner.GetAsync($"/api/owner/session-reviews?horseId={horseId}"), HttpStatusCode.NotFound);
        Assert.Contains(
            (await OwnerReviews(Owner())).EnumerateArray().ToList(),
            r => r.GetProperty("sessionId").GetInt32() == id);
    }

    [Fact]
    public async Task Owner_CanFilterByHorseDateAndLimit()
    {
        var (first, horseId) = NewSession();
        await Read(await Review(f.Trainer(), first, Body(6)));
        await Read(await Send(f.Trainer(), first));

        var today = TestData.Today.ToString("yyyy-MM-dd");
        Assert.Single((await OwnerReviews(Owner(), $"?horseId={horseId}&from={today}&to={today}")).EnumerateArray().ToList());
        Assert.Empty((await OwnerReviews(Owner(), $"?horseId={horseId}&from={TestData.Today.AddDays(1):yyyy-MM-dd}")).EnumerateArray().ToList());
        Assert.Single((await OwnerReviews(Owner(), "?limit=1")).EnumerateArray().ToList());
        await Read(await Owner().GetAsync($"/api/owner/session-reviews?from={today}&to={TestData.Today.AddDays(-1):yyyy-MM-dd}"), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Groom_DoesNotSeeReview_InMyAssignments()
    {
        var (id, _) = NewSession();
        await Read(await Review(f.Trainer(), id, Body()));
        await Read(await Send(f.Trainer(), id));

        var range = $"from={TestData.Today:yyyy-MM-dd}&to={TestData.Today:yyyy-MM-dd}";
        var rows = await Read(await f.Groom().GetAsync($"/api/training-assignments/me?{range}"));
        var row = rows.EnumerateArray().Single(s => s.GetProperty("sessionId").GetInt32() == id);
        Assert.Equal(JsonValueKind.Null, row.GetProperty("review").ValueKind);
        Assert.Equal("Completed", row.GetProperty("report").GetProperty("result").GetString());
    }

    [Theory]
    [InlineData("Groom")]
    [InlineData("Owner")]
    [InlineData("Anonymous")]
    public async Task OtherRoles_CannotReviewOrSend(string who)
    {
        var (id, _) = NewSession();
        var client = who switch
        {
            "Groom" => f.Groom(),
            "Owner" => Owner(),
            _ => f.Anonymous(),
        };
        var expected = who == "Anonymous" ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden;
        Assert.Equal(expected, (await Review(client, id, Body())).StatusCode);
        Assert.Equal(expected, (await EditReview(client, id, Body())).StatusCode);
        Assert.Equal(expected, (await Send(client, id)).StatusCode);
    }

    [Theory]
    [InlineData("Trainer")]
    [InlineData("Groom")]
    [InlineData("Anonymous")]
    public async Task OnlyOwners_CanReadOwnerReviews(string who)
    {
        var client = who switch
        {
            "Trainer" => f.Trainer(),
            "Groom" => f.Groom(),
            _ => f.Anonymous(),
        };
        var response = await client.GetAsync("/api/owner/session-reviews");
        Assert.Equal(who == "Anonymous" ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden, response.StatusCode);
    }
}
