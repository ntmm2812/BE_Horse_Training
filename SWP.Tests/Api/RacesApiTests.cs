using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SWP.Domain.Models.Entity;
using SWP.Tests.Support;

namespace SWP.Tests.Api;

public class RacesApiTests : IClassFixture<TrainingApiFactory>
{
    private readonly TrainingApiFactory f;
    public RacesApiTests(TrainingApiFactory factory) => f = factory;

    private HttpClient Manager => f.ClientAs(f.Ids.ManagerId, "Club Manager");

    private static async Task<JsonElement> Read(HttpResponseMessage response, HttpStatusCode expected)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == expected, $"{response.StatusCode}: {body}");
        return string.IsNullOrEmpty(body) ? default : JsonSerializer.Deserialize<JsonElement>(body);
    }

    private static string Day(int offset) => TestData.Today.AddDays(offset).ToString("yyyy-MM-dd");

    private static object Body(
        string? tenGiai = "Cúp Mùa Thu",
        string? ngay = null,
        string? deadline = null,
        int? distance = 1600,
        int? max = 12,
        decimal? prize = 50_000_000,
        string? description = "Ngựa từ 3 tuổi trở lên.",
        decimal? entryFee = null,
        decimal? first = null,
        decimal? second = null,
        decimal? third = null) => new
    {
        entryFee,
        prizeFirst = first,
        prizeSecond = second,
        prizeThird = third,
        tenGiai,
        diaDiem = "Trường đua Phú Thọ",
        ngay = ngay ?? Day(14),
        registrationDeadline = deadline ?? Day(7),
        distanceMeters = distance,
        maxParticipants = max,
        prizePool = prize,
        description,
    };

    [Fact]
    public async Task Manager_CreatesRace_ItIsOpen_AndShowsInRaceList()
    {
        var created = await Read(await Manager.PostAsJsonAsync("/api/races", Body()), HttpStatusCode.Created);

        Assert.Equal("Cúp Mùa Thu", created.GetProperty("tenGiai").GetString());
        Assert.Equal("Open", created.GetProperty("status").GetString());
        Assert.Equal(Day(14), created.GetProperty("ngay").GetString());
        Assert.Equal(Day(7), created.GetProperty("registrationDeadline").GetString());
        Assert.Equal(1600, created.GetProperty("distanceMeters").GetInt32());
        Assert.Equal(12, created.GetProperty("maxParticipants").GetInt32());
        Assert.Equal(50_000_000m, created.GetProperty("prizePool").GetDecimal());
        var id = created.GetProperty("raceID").GetInt32();

        // Trainer thấy giải trong danh sách để đăng ký
        var list = await Read(await f.Trainer().GetAsync("/api/races"), HttpStatusCode.OK);
        Assert.Contains(list.EnumerateArray(), r => r.GetProperty("raceID").GetInt32() == id);
    }

    [Fact]
    public async Task TwoRaces_CanBeCreated_WithoutRaceCodeCollision()
    {
        await Read(await Manager.PostAsJsonAsync("/api/races", Body("Giải A")), HttpStatusCode.Created);
        await Read(await Manager.PostAsJsonAsync("/api/races", Body("Giải B")), HttpStatusCode.Created);
    }

    [Theory]
    [InlineData("Head Trainer")]
    [InlineData("Horse Owner")]
    [InlineData("Veterinarian")]
    public async Task OtherRoles_CannotCreateRace(string role)
    {
        var client = f.ClientAs(f.Ids.TrainerId, role);
        var response = await client.PostAsJsonAsync("/api/races", Body());
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotCreateRace()
    {
        var response = await f.Anonymous().PostAsJsonAsync("/api/races", Body());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task RaceDate_PastOrToday_IsRejected(int offset)
    {
        var error = await Read(
            await Manager.PostAsJsonAsync("/api/races", Body(ngay: Day(offset), deadline: Day(0))),
            HttpStatusCode.BadRequest);
        Assert.Equal("VALIDATION_ERROR", error.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Deadline_AfterRaceDate_IsRejected()
    {
        await Read(
            await Manager.PostAsJsonAsync("/api/races", Body(ngay: Day(10), deadline: Day(11))),
            HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Deadline_InThePast_IsRejected()
    {
        await Read(
            await Manager.PostAsJsonAsync("/api/races", Body(deadline: Day(-1))),
            HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Deadline_OnRaceDateOrToday_IsAccepted()
    {
        await Read(
            await Manager.PostAsJsonAsync("/api/races", Body(ngay: Day(5), deadline: Day(0))),
            HttpStatusCode.Created);
        await Read(
            await Manager.PostAsJsonAsync("/api/races", Body(ngay: Day(5), deadline: Day(5))),
            HttpStatusCode.Created);
    }

    [Theory]
    [InlineData("", 1600, 12)]
    [InlineData("   ", 1600, 12)]
    [InlineData("Giải", 0, 12)]
    [InlineData("Giải", -100, 12)]
    [InlineData("Giải", null, 12)]
    [InlineData("Giải", 1600, 0)]
    [InlineData("Giải", 1600, 51)]
    [InlineData("Giải", 1600, null)]
    public async Task InvalidNameDistanceOrCapacity_IsRejected(string name, int? distance, int? max)
    {
        var error = await Read(
            await Manager.PostAsJsonAsync("/api/races", Body(tenGiai: name, distance: distance, max: max)),
            HttpStatusCode.BadRequest);
        Assert.False(string.IsNullOrEmpty(error.GetProperty("message").GetString()));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    public async Task Capacity_BoundsAreInclusive(int max)
    {
        await Read(await Manager.PostAsJsonAsync("/api/races", Body(max: max)), HttpStatusCode.Created);
    }

    [Fact]
    public async Task EntryFeeAndRankPrizes_AreSaved_AndTotalIsRecomputedByBackend()
    {
        // prizePool = 1 do FE gửi bị bỏ qua: tổng luôn = Nhất + Nhì + Ba
        var created = await Read(
            await Manager.PostAsJsonAsync("/api/races", Body(prize: 1, entryFee: 200_000, first: 100_000_000, second: 50_000_000, third: 20_000_000)),
            HttpStatusCode.Created);

        Assert.Equal(200_000m, created.GetProperty("entryFee").GetDecimal());
        Assert.Equal(100_000_000m, created.GetProperty("prizeFirst").GetDecimal());
        Assert.Equal(50_000_000m, created.GetProperty("prizeSecond").GetDecimal());
        Assert.Equal(20_000_000m, created.GetProperty("prizeThird").GetDecimal());
        Assert.Equal(170_000_000m, created.GetProperty("prizePool").GetDecimal());

        var id = created.GetProperty("raceID").GetInt32();
        var list = await Read(await f.Trainer().GetAsync("/api/races"), HttpStatusCode.OK);
        var row = list.EnumerateArray().Single(r => r.GetProperty("raceID").GetInt32() == id);
        Assert.Equal(170_000_000m, row.GetProperty("prizePool").GetDecimal());
        Assert.Equal(50_000_000m, row.GetProperty("prizeSecond").GetDecimal());
    }

    [Fact]
    public async Task OnlyFirstPrize_TotalEqualsIt_AndEqualPrizesAreAllowed()
    {
        var one = await Read(await Manager.PostAsJsonAsync("/api/races", Body(prize: 5, first: 30_000_000)), HttpStatusCode.Created);
        Assert.Equal(30_000_000m, one.GetProperty("prizePool").GetDecimal());
        Assert.Equal(JsonValueKind.Null, one.GetProperty("prizeSecond").ValueKind);

        var equal = await Read(
            await Manager.PostAsJsonAsync("/api/races", Body(first: 10_000_000, second: 10_000_000, third: 10_000_000)),
            HttpStatusCode.Created);
        Assert.Equal(30_000_000m, equal.GetProperty("prizePool").GetDecimal());
    }

    [Fact]
    public async Task RaceWithoutRankPrizes_KeepsManualTotal_AndFeeDefaultsToZero()
    {
        var created = await Read(await Manager.PostAsJsonAsync("/api/races", Body(prize: 80_000_000)), HttpStatusCode.Created);

        Assert.Equal(80_000_000m, created.GetProperty("prizePool").GetDecimal());
        Assert.Equal(JsonValueKind.Null, created.GetProperty("prizeFirst").ValueKind);
        Assert.Equal(JsonValueKind.Null, created.GetProperty("prizeSecond").ValueKind);
        Assert.Equal(JsonValueKind.Null, created.GetProperty("prizeThird").ValueKind);
        Assert.Equal(0m, created.GetProperty("entryFee").GetDecimal());
    }

    [Theory]
    [InlineData(100, 200, null)] // Nhì > Nhất
    [InlineData(200, 100, 150)] // Ba > Nhì
    [InlineData(100, null, 200)] // Ba > Nhất khi bỏ trống Nhì
    [InlineData(-1, null, null)]
    [InlineData(null, -5, null)]
    [InlineData(null, null, -1)]
    public async Task InvalidRankPrizes_AreRejected(int? first, int? second, int? third)
    {
        var error = await Read(
            await Manager.PostAsJsonAsync("/api/races", Body(first: first, second: second, third: third)),
            HttpStatusCode.BadRequest);
        Assert.Equal("VALIDATION_ERROR", error.GetProperty("code").GetString());
    }

    [Fact]
    public async Task NegativeEntryFee_IsRejected()
    {
        await Read(await Manager.PostAsJsonAsync("/api/races", Body(entryFee: -1)), HttpStatusCode.BadRequest);
    }

    // --- Hủy giải ---

    private int InsertRace(int dayOffset, string status = "Open", bool withResult = false) => f.WithDb(db =>
    {
        var race = new Race
        {
            RaceName = "Giải thử hủy",
            RaceCode = $"T-{Guid.NewGuid():N}"[..14],
            RaceDate = TestData.Today.AddDays(dayOffset).ToDateTime(TimeOnly.MinValue),
            RegistrationDeadline = TestData.Today.AddDays(dayOffset - 7).ToDateTime(TimeOnly.MinValue),
            MaxParticipants = 10,
            Status = status,
            CreatedAt = TestData.Now.UtcDateTime,
        };
        db.Races.Add(race);
        db.SaveChanges();
        if (withResult)
        {
            var horseId = TestData.AddHorse(db, f.Ids.OwnerId, $"Result horse {Guid.NewGuid():N}");
            db.RaceRegistrations.Add(new RaceRegistration
            {
                RaceId = race.RaceId,
                HorseId = horseId,
                RegistrationDate = TestData.Today.AddDays(dayOffset - 3).ToDateTime(TimeOnly.MinValue),
                RegistrationStatus = "Registered",
                FinishPosition = 1,
                CreatedAt = TestData.Now.UtcDateTime,
            });
            db.SaveChanges();
        }

        return race.RaceId;
    });

    private Task<HttpResponseMessage> Cancel(HttpClient client, int raceId, string? reason = "Thời tiết xấu") =>
        client.PostAsJsonAsync($"/api/races/{raceId}/cancel", new { reason });

    private async Task<JsonElement> Proposal(int id) =>
        await Read(await f.Trainer().GetAsync($"/api/approvals/races/{id}"), HttpStatusCode.OK);

    [Fact]
    public async Task Manager_CancelsRace_StatusChanges_AndPendingRegistrationsAreClosed()
    {
        var race = await Read(await Manager.PostAsJsonAsync("/api/races", Body("Giải sẽ hủy")), HttpStatusCode.Created);
        var raceId = race.GetProperty("raceID").GetInt32();

        var draftId = await NewProposal(raceId);
        var submittedId = await NewProposal(raceId);
        var submit = await f.Trainer().PostAsync($"/api/approvals/races/{submittedId}/submit", null);
        Assert.True(submit.IsSuccessStatusCode, await submit.Content.ReadAsStringAsync());
        Assert.Equal("PendingOwner", (await Proposal(submittedId)).GetProperty("status").GetString());

        var cancelled = await Read(await Cancel(Manager, raceId), HttpStatusCode.OK);
        Assert.Equal("Cancelled", cancelled.GetProperty("status").GetString());

        var list = await Read(await f.Trainer().GetAsync("/api/races"), HttpStatusCode.OK);
        var row = list.EnumerateArray().Single(r => r.GetProperty("raceID").GetInt32() == raceId);
        Assert.Equal("Cancelled", row.GetProperty("status").GetString());

        foreach (var id in new[] { draftId, submittedId })
        {
            var proposal = await Proposal(id);
            Assert.Equal("Rejected", proposal.GetProperty("status").GetString());
            Assert.Equal("Giải đã hủy", proposal.GetProperty("rejectReason").GetString());
            Assert.Equal("Manager", proposal.GetProperty("rejectedBy").GetString());
            Assert.False(proposal.GetProperty("trainerSeen").GetBoolean());
            Assert.Contains("Thời tiết xấu", proposal.GetProperty("history").EnumerateArray().Last().GetProperty("note").GetString());
        }

        // Giải đã hủy không nhận đăng ký mới
        var again = await f.Trainer().PostAsJsonAsync("/api/approvals/races", new { horseId = f.AddHorse("Late horse"), raceId, note = "x" });
        Assert.False(again.IsSuccessStatusCode);
    }

    private async Task<int> NewProposal(int raceId)
    {
        var response = await f.Trainer().PostAsJsonAsync("/api/approvals/races", new { horseId = f.AddHorse($"Cancel horse {Guid.NewGuid():N}"), raceId, note = "" });
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
        return JsonSerializer.Deserialize<JsonElement>(body).GetProperty("id").GetInt32();
    }

    [Theory]
    [InlineData("Head Trainer")]
    [InlineData("Horse Owner")]
    [InlineData("Veterinarian")]
    public async Task OtherRoles_CannotCancelRace(string role)
    {
        var raceId = InsertRace(10);
        var response = await Cancel(f.ClientAs(f.Ids.TrainerId, role), raceId);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var list = await Read(await Manager.GetAsync("/api/races"), HttpStatusCode.OK);
        Assert.Equal("Open", list.EnumerateArray().Single(r => r.GetProperty("raceID").GetInt32() == raceId).GetProperty("status").GetString());
    }

    [Fact]
    public async Task PastRace_CannotBeCancelled()
    {
        var raceId = InsertRace(-2);
        var error = await Read(await Cancel(Manager, raceId), HttpStatusCode.Conflict);
        Assert.Equal("CONFLICT", error.GetProperty("code").GetString());
    }

    [Fact]
    public async Task RaceWithResults_CannotBeCancelled()
    {
        var raceId = InsertRace(5, withResult: true);
        await Read(await Cancel(Manager, raceId), HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AlreadyCancelledRace_CannotBeCancelledAgain()
    {
        var raceId = InsertRace(10);
        await Read(await Cancel(Manager, raceId), HttpStatusCode.OK);
        await Read(await Cancel(Manager, raceId), HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CancelWithoutReason_IsRejected_AndRaceStaysOpen(string? reason)
    {
        var raceId = InsertRace(10);
        var error = await Read(await Cancel(Manager, raceId, reason), HttpStatusCode.BadRequest);
        Assert.Equal("VALIDATION_ERROR", error.GetProperty("code").GetString());

        var list = await Read(await Manager.GetAsync("/api/races"), HttpStatusCode.OK);
        Assert.Equal("Open", list.EnumerateArray().Single(r => r.GetProperty("raceID").GetInt32() == raceId).GetProperty("status").GetString());
    }

    [Fact]
    public async Task CancelReason_OverLimit_IsRejected_AndBoundaryIsAccepted()
    {
        var raceId = InsertRace(10);
        await Read(await Cancel(Manager, raceId, new string('x', 501)), HttpStatusCode.BadRequest);
        await Read(await Cancel(Manager, raceId, new string('x', 500)), HttpStatusCode.OK);
    }

    [Fact]
    public async Task CancelUnknownRace_ReturnsNotFound()
    {
        await Read(await Cancel(Manager, 999999), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Result_AndPrize_AreManagerOnly()
    {
        var result = new { thuHang = 1 };
        var prize = new { amount = 1_000_000 };

        // Trainer bị chặn ở bước phân quyền (403) trước khi tới nghiệp vụ
        Assert.Equal(HttpStatusCode.Forbidden, (await f.Trainer().PutAsJsonAsync("/api/races/999999/registrations/1/result", result)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await f.Trainer().PutAsJsonAsync("/api/races/999999/registrations/1/prize", prize)).StatusCode);

        // Quản lý qua được phân quyền và nhận 404 vì giải không tồn tại
        Assert.Equal(HttpStatusCode.NotFound, (await Manager.PutAsJsonAsync("/api/races/999999/registrations/1/result", result)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Manager.PutAsJsonAsync("/api/races/999999/registrations/1/prize", prize)).StatusCode);
    }
}
