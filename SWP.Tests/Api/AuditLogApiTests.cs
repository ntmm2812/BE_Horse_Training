using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SWP.Tests.Support;

namespace SWP.Tests.Api;

public class AuditLogApiTests : IClassFixture<TrainingApiFactory>
{
    private readonly TrainingApiFactory f;
    public AuditLogApiTests(TrainingApiFactory factory) => f = factory;
    private HttpClient Manager => f.ClientAs(f.Ids.ManagerId, "Club Manager");

    private static async Task<JsonElement> Read(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == expected, $"{response.StatusCode}: {body}");
        return string.IsNullOrEmpty(body) ? default : JsonSerializer.Deserialize<JsonElement>(body);
    }

    private static Task<HttpResponseMessage> Post(HttpClient client, object body) =>
        client.PostAsJsonAsync("/api/audit-logs", body);

    [Fact]
    public async Task Post_AnyRole_UsesUserIdFromToken_IgnoringClientUserId()
    {
        var marker = $"Ghi nhật ký {Guid.NewGuid():N}";
        var response = await Post(f.Trainer(), new
        {
            userId = f.Ids.ManagerId,
            action = marker,
            targetTable = "horse",
            targetId = 7,
            targetLabel = "Thunder",
        });
        await Read(response, HttpStatusCode.NoContent);

        var page = await Read(await Manager.GetAsync($"/api/manager/audit-logs?keyword={Uri.EscapeDataString(marker)}"));
        var item = Assert.Single(page.GetProperty("items").EnumerateArray());
        Assert.Equal(f.Ids.TrainerId, item.GetProperty("userId").GetInt32());
        Assert.Equal(marker, item.GetProperty("action").GetString());
        Assert.Equal("HORSE", item.GetProperty("targetTable").GetString());
        Assert.Equal(7, item.GetProperty("targetId").GetInt32());
        Assert.Equal("Thunder", item.GetProperty("targetLabel").GetString());
        Assert.False(string.IsNullOrEmpty(item.GetProperty("actorName").GetString()));
    }

    [Fact]
    public async Task Post_RejectsAnonymous_AndInvalidBodies()
    {
        await Read(await Post(f.Anonymous(), new { action = "x", targetTable = "HORSE" }), HttpStatusCode.Unauthorized);
        await Read(await Post(f.Groom(), new { action = " ", targetTable = "HORSE" }), HttpStatusCode.BadRequest);
        await Read(await Post(f.Groom(), new { action = new string('a', 301), targetTable = "HORSE" }), HttpStatusCode.BadRequest);
        await Read(await Post(f.Groom(), new { action = "x", targetTable = "USERS; DROP" }), HttpStatusCode.BadRequest);
        await Read(await Post(f.Groom(), new { action = "x", targetTable = "HORSE", targetLabel = new string('b', 201) }), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_IsClubManagerOnly()
    {
        await Read(await f.Anonymous().GetAsync("/api/manager/audit-logs"), HttpStatusCode.Unauthorized);
        await Read(await f.Trainer().GetAsync("/api/manager/audit-logs"), HttpStatusCode.Forbidden);
        await Read(await f.Groom().GetAsync("/api/manager/audit-logs"), HttpStatusCode.Forbidden);
        await Read(await f.ClientAs(f.Ids.OwnerId, "Horse Owner").GetAsync("/api/manager/audit-logs"), HttpStatusCode.Forbidden);
        await Read(await Manager.GetAsync("/api/manager/audit-logs"));
    }

    [Fact]
    public async Task Get_FiltersByUserKeywordAndDate_AndPaginatesNewestFirst()
    {
        var marker = Guid.NewGuid().ToString("N");
        for (var i = 1; i <= 5; i++)
        {
            await Read(await Post(f.Groom(), new { action = $"Việc {marker} số {i}", targetTable = "CARETASK", targetId = i }), HttpStatusCode.NoContent);
        }

        await Read(await Post(f.Trainer(), new { action = $"Việc {marker} của HLV", targetTable = "TRAININGPLAN" }), HttpStatusCode.NoContent);

        var q = $"keyword={marker}";
        var all = await Read(await Manager.GetAsync($"/api/manager/audit-logs?{q}"));
        Assert.Equal(6, all.GetProperty("total").GetInt32());

        var byUser = await Read(await Manager.GetAsync($"/api/manager/audit-logs?{q}&userId={f.Ids.GroomId}"));
        Assert.Equal(5, byUser.GetProperty("total").GetInt32());

        var first = await Read(await Manager.GetAsync($"/api/manager/audit-logs?{q}&userId={f.Ids.GroomId}&page=1&pageSize=2"));
        var second = await Read(await Manager.GetAsync($"/api/manager/audit-logs?{q}&userId={f.Ids.GroomId}&page=2&pageSize=2"));
        var firstIds = first.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("logId").GetInt64()).ToList();
        var secondIds = second.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("logId").GetInt64()).ToList();
        Assert.Equal(2, firstIds.Count);
        Assert.Equal(2, secondIds.Count);
        Assert.True(firstIds.Min() > secondIds.Max(), "Mới nhất trước, trang sau cũ hơn");

        var today = TestData.Today;
        var inRange = await Read(await Manager.GetAsync($"/api/manager/audit-logs?{q}&from={today:yyyy-MM-dd}&to={today:yyyy-MM-dd}"));
        Assert.Equal(6, inRange.GetProperty("total").GetInt32());
        var past = await Read(await Manager.GetAsync($"/api/manager/audit-logs?{q}&to={today.AddDays(-1):yyyy-MM-dd}"));
        Assert.Equal(0, past.GetProperty("total").GetInt32());
        var future = await Read(await Manager.GetAsync($"/api/manager/audit-logs?{q}&from={today.AddDays(1):yyyy-MM-dd}"));
        Assert.Equal(0, future.GetProperty("total").GetInt32());

        await Read(await Manager.GetAsync($"/api/manager/audit-logs?from={today:yyyy-MM-dd}&to={today.AddDays(-1):yyyy-MM-dd}"), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_ClampsPageSize()
    {
        var page = await Read(await Manager.GetAsync("/api/manager/audit-logs?pageSize=5000&page=0"));
        Assert.Equal(100, page.GetProperty("pageSize").GetInt32());
        Assert.Equal(1, page.GetProperty("page").GetInt32());
    }
}
