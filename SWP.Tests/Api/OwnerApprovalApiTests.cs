using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SWP.Application.DTOs.Training;
using SWP.Domain.Common;
using SWP.Domain.Models.Entity;
using SWP.Tests.Support;

namespace SWP.Tests.Api;

public class OwnerApprovalApiTests : IClassFixture<TrainingApiFactory>
{
    private readonly TrainingApiFactory f;
    public OwnerApprovalApiTests(TrainingApiFactory factory) => f = factory;
    private HttpClient Owner => f.ClientAs(f.Ids.OwnerId, "Horse Owner");
    private HttpClient Manager => f.ClientAs(f.Ids.ManagerId, "Club Manager");
    private static async Task<JsonElement> Read(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == expected, $"{response.StatusCode}: {body}");
        return string.IsNullOrEmpty(body) ? default : JsonSerializer.Deserialize<JsonElement>(body);
    }
    private async Task<int> DraftPlan()
    {
        var request = TestData.ThreePhasePlan(f.AddHorse("Approval horse"));
        request.Notes = "PRIVATE TRAINER NOTES";
        var plan = await Read(await f.Trainer().PostAsJsonAsync("/api/trainer/training-plans", request), HttpStatusCode.Created);
        return plan.GetProperty("planId").GetInt32();
    }
    private async Task<JsonElement> SendPlan(int id) => await Read(await f.Trainer().PostAsync($"/api/approvals/plans/{id}/send", null));
    private async Task<JsonElement> DecidePlan(int id, JsonElement record, string status = "Approved", string? note = null) =>
        await Read(await Owner.PostAsJsonAsync($"/api/approvals/plans/{id}/decision", new { status, note, version = record.GetProperty("version").GetString() }));

    [Fact]
    public async Task GroomAndWeeklySchedule_PersistAcrossRequests_AndShareOneDatabase()
    {
        var id = await DraftPlan();
        await DecidePlan(id, await SendPlan(id));
        var active = await Read(await f.Trainer().PostAsJsonAsync($"/api/trainer/training-plans/{id}/activate", new { assignedToUserId = f.Ids.GroomId }));
        Assert.Equal(f.Ids.GroomId, active.GetProperty("defaultAssignedToUserId").GetInt32());
        var freshRead = await Read(await f.Trainer().GetAsync($"/api/trainer/training-plans/{id}"));
        Assert.Equal(f.Ids.GroomId, freshRead.GetProperty("defaultAssignedToUserId").GetInt32());
        Assert.False(string.IsNullOrEmpty(freshRead.GetProperty("defaultAssignedToName").GetString()));
        var generated = await Read(await f.Trainer().PostAsJsonAsync($"/api/trainer/training-plans/{id}/weekly-schedule",
            new { weekStartDate = TestData.Today, previewOnly = false }), HttpStatusCode.Created);
        var sessions = generated.GetProperty("sessions").EnumerateArray().ToArray();
        Assert.NotEmpty(sessions);
        Assert.All(sessions, s => Assert.Equal(f.Ids.GroomId, s.GetProperty("assignedToUserId").GetInt32()));
        var groomRows = await Read(await f.Groom().GetAsync($"/api/training-assignments/me?from={TestData.Today:yyyy-MM-dd}&to={TestData.Today.AddDays(6):yyyy-MM-dd}"));
        Assert.Contains(groomRows.EnumerateArray(), s => s.GetProperty("planId").GetInt32() == id);
        var managerRows = await Read(await Manager.GetAsync("/api/training-overview/schedules"));
        var ownerRows = await Read(await Owner.GetAsync("/api/training-overview/schedules"));
        foreach (var session in sessions)
        {
            var sid = session.GetProperty("sessionId").GetInt32();
            Assert.Contains(managerRows.EnumerateArray(), s => s.GetProperty("scheduleID").GetInt32() == sid);
            Assert.Contains(ownerRows.EnumerateArray(), s => s.GetProperty("scheduleID").GetInt32() == sid);
        }
        var otherOwner = await Read(await f.ClientAs(f.Ids.ManagerId, "Horse Owner").GetAsync("/api/training-overview/schedules"));
        Assert.DoesNotContain(otherOwner.EnumerateArray(), s => s.GetProperty("planId").GetInt32() == id);
    }

    [Fact]
    public async Task ManualSession_InheritsStoredGroom_AndExplicitUnassignedIsHonoured()
    {
        var id = await DraftPlan();
        await DecidePlan(id, await SendPlan(id));
        await Read(await f.Trainer().PostAsJsonAsync($"/api/trainer/training-plans/{id}/activate", new { assignedToUserId = f.Ids.GroomId }));
        var created = await Read(await f.Trainer().PostAsJsonAsync($"/api/trainer/training-plans/{id}/sessions",
            TestData.Session(TestData.Today.AddDays(1), "Trot")), HttpStatusCode.Created);
        Assert.Equal(f.Ids.GroomId, created.GetProperty("assignedToUserId").GetInt32());
        var unassigned = await Read(await f.Trainer().PostAsJsonAsync($"/api/trainer/training-plans/{id}/weekly-schedule",
            new { weekStartDate = TestData.Today, previewOnly = true, usePlanAssignee = false }), HttpStatusCode.OK);
        Assert.All(unassigned.GetProperty("sessions").EnumerateArray(), s => Assert.Equal(JsonValueKind.Null, s.GetProperty("assignedToUserId").ValueKind));
    }

    [Theory]
    [InlineData("owner")]
    [InlineData("pending")]
    [InlineData("trainer")]
    [InlineData("unknown")]
    public async Task Activation_RejectsInvalidGroomWithoutActivating(string scenario)
    {
        var id = await DraftPlan();
        await DecidePlan(id, await SendPlan(id));
        var userId = scenario switch { "owner" => f.Ids.OwnerId, "pending" => f.Ids.PendingGroomId, "trainer" => f.Ids.TrainerId, _ => 999999 };
        await Read(await f.Trainer().PostAsJsonAsync($"/api/trainer/training-plans/{id}/activate", new { assignedToUserId = userId }), HttpStatusCode.BadRequest);
        var plan = await Read(await f.Trainer().GetAsync($"/api/trainer/training-plans/{id}"));
        Assert.Equal("Draft", plan.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, plan.GetProperty("defaultAssignedToUserId").ValueKind);
    }

    [Fact]
    public async Task Plan_IsSharedBetweenSessions_PrivateAndEnforcedByBackend()
    {
        var id = await DraftPlan();
        await Read(await f.Trainer().PostAsync($"/api/trainer/training-plans/{id}/activate", null), HttpStatusCode.Conflict);
        await Read(await Owner.GetAsync($"/api/approvals/plans/{id}"), HttpStatusCode.NotFound);
        var sent = await SendPlan(id);
        var ownerRecord = await Read(await Owner.GetAsync($"/api/approvals/plans/{id}"));
        Assert.Equal("PendingOwner", ownerRecord.GetProperty("status").GetString());
        Assert.DoesNotContain("PRIVATE TRAINER NOTES", ownerRecord.GetRawText());
        Assert.False(ownerRecord.GetProperty("ownerSeen").GetBoolean());
        await Read(await f.ClientAs(f.Ids.ManagerId, "Horse Owner").GetAsync($"/api/approvals/plans/{id}"), HttpStatusCode.NotFound);
        var approved = await DecidePlan(id, sent);
        Assert.Equal("Approved", approved.GetProperty("status").GetString());
        Assert.False(approved.GetProperty("trainerSeen").GetBoolean());
        await Read(await f.Trainer().PostAsync($"/api/trainer/training-plans/{id}/activate", null));
        var session = await Read(await f.Trainer().PostAsJsonAsync($"/api/trainer/training-plans/{id}/sessions",
            TestData.Session(TestData.Today.AddDays(1), "Trot")), HttpStatusCode.Created);
        var sessions = await Read(await Owner.GetAsync($"/api/approvals/owner-sessions?from={TestData.Today:yyyy-MM-dd}&to={TestData.Today.AddDays(7):yyyy-MM-dd}"));
        Assert.Contains(sessions.EnumerateArray(), s => s.GetProperty("planId").GetInt32() == id);
        Assert.DoesNotContain("trainerComment", sessions.GetRawText());
    }

    [Fact]
    public async Task EditingSubmittedPlan_InvalidatesStaleDecision_AndRequiresReapproval()
    {
        var id = await DraftPlan();
        var sent = await SendPlan(id);
        var detail = await f.Trainer().GetFromJsonAsync<TrainingPlanDetailDto>($"/api/trainer/training-plans/{id}");
        await Read(await f.Trainer().PutAsJsonAsync($"/api/trainer/training-plans/{id}", new { planName = "Updated public plan", objective = "New goal", detail!.TrainingType, phases = (object?)null }));
        await Read(await Owner.PostAsJsonAsync($"/api/approvals/plans/{id}/decision", new { status = "Approved", version = sent.GetProperty("version").GetString() }), HttpStatusCode.Conflict);
        var current = await Read(await Owner.GetAsync($"/api/approvals/plans/{id}"));
        var approved = await DecidePlan(id, current);
        await Read(await f.Trainer().PutAsJsonAsync($"/api/trainer/training-plans/{id}", new { planName = "Updated again", phases = (object?)null }));
        current = await Read(await Owner.GetAsync($"/api/approvals/plans/{id}"));
        Assert.Equal("PendingOwner", current.GetProperty("status").GetString());
        Assert.NotEqual(approved.GetProperty("version").GetString(), current.GetProperty("version").GetString());
        await Read(await f.Trainer().PostAsync($"/api/trainer/training-plans/{id}/activate", null), HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("Rejected")]
    [InlineData("ChangesRequested")]
    public async Task NegativePlanDecision_RequiresReason_AndPersistsIt(string status)
    {
        var id = await DraftPlan();
        var sent = await SendPlan(id);
        await Read(await Owner.PostAsJsonAsync($"/api/approvals/plans/{id}/decision", new { status, note = " ", version = sent.GetProperty("version").GetString() }), HttpStatusCode.BadRequest);
        var decided = await DecidePlan(id, sent, status, "Cần giảm cường độ");
        Assert.Equal(status, decided.GetProperty("status").GetString());
        var seenByTrainer = await Read(await f.Trainer().GetAsync($"/api/approvals/plans/{id}"));
        Assert.Equal("Cần giảm cường độ", seenByTrainer.GetProperty("note").GetString());
    }

    [Fact]
    public async Task PlanReadFlags_ArePersistedAndScopedToCurrentOwner()
    {
        var id = await DraftPlan();
        await SendPlan(id);
        await Read(await f.ClientAs(f.Ids.ManagerId, "Horse Owner").PostAsJsonAsync("/api/approvals/plans/read", new { planId = id }), HttpStatusCode.NoContent);
        var record = await Read(await Owner.GetAsync($"/api/approvals/plans/{id}"));
        Assert.False(record.GetProperty("ownerSeen").GetBoolean());
        await Read(await Owner.PostAsJsonAsync("/api/approvals/plans/read", new { planId = id }), HttpStatusCode.NoContent);
        record = await Read(await Owner.GetAsync($"/api/approvals/plans/{id}"));
        Assert.True(record.GetProperty("ownerSeen").GetBoolean());
    }

    private int AddRace(int days = 14, int capacity = 10) => f.WithDb(db =>
    {
        var race = new Race { RaceName = "Test race", RaceCode = Guid.NewGuid().ToString(), Status = "Open", CreatedAt = TestData.Now.UtcDateTime,
            RaceDate = TestData.Today.AddDays(days).ToDateTime(TimeOnly.MinValue), RegistrationDeadline = TestData.Today.AddDays(days - 7).ToDateTime(TimeOnly.MinValue), EntryFee = 1234567, MaxParticipants = capacity };
        db.Races.Add(race); db.SaveChanges(); return race.RaceId;
    });
    private async Task<JsonElement> Proposal(int raceId, int? horseId = null) =>
        await Read(await f.Trainer().PostAsJsonAsync("/api/approvals/races", new { horseId = horseId ?? f.AddHorse("Race approval horse"), raceId, note = "Đăng ký thử" }));
    private async Task<JsonElement> Submit(JsonElement proposal) => await Read(await f.Trainer().PostAsync($"/api/approvals/races/{proposal.GetProperty("id").GetInt32()}/submit", null));
    private async Task<JsonElement> DecideRace(HttpClient client, JsonElement record, bool approve = true, string? note = null, HttpStatusCode code = HttpStatusCode.OK) =>
        await Read(await client.PostAsJsonAsync($"/api/approvals/races/{record.GetProperty("id").GetInt32()}/decision", new { approve, note, version = record.GetProperty("version").GetString() }), code);

    [Fact]
    public async Task Race_RequiresOwnerThenManager_AndCreatesExactlyOneRealEntry()
    {
        var raceId = AddRace();
        var proposal = await Proposal(raceId);
        await Read(await f.Trainer().PostAsJsonAsync($"/api/races/{raceId}/registrations", new { horseID = proposal.GetProperty("horseId").GetInt32() }), HttpStatusCode.Conflict);
        var id = proposal.GetProperty("id").GetInt32();
        await Read(await Owner.GetAsync($"/api/approvals/races/{id}"), HttpStatusCode.NotFound);
        var sent = await Submit(proposal);
        Assert.Equal(1234567, sent.GetProperty("entryFee").GetDecimal());
        await DecideRace(Manager, sent, code: HttpStatusCode.Conflict);
        await DecideRace(f.ClientAs(f.Ids.ManagerId, "Horse Owner"), sent, code: HttpStatusCode.NotFound);
        var ownerApproved = await DecideRace(Owner, sent);
        Assert.Equal("PendingManager", ownerApproved.GetProperty("status").GetString());
        Assert.False(f.WithDb(db => db.RaceRegistrations.Any(r => r.RaceId == raceId)));
        var final = await DecideRace(Manager, ownerApproved);
        Assert.Equal("Approved", final.GetProperty("status").GetString());
        Assert.Equal(1, f.WithDb(db => db.RaceRegistrations.Count(r => r.RaceId == raceId)));
        await DecideRace(Manager, ownerApproved, code: HttpStatusCode.Conflict);
        var entries = await Read(await Owner.GetAsync("/api/races/registrations"));
        Assert.Contains(entries.EnumerateArray(), r => r.GetProperty("raceID").GetInt32() == raceId);
        await Read(await f.Trainer().DeleteAsync($"/api/races/{raceId}/registrations/{proposal.GetProperty("horseId").GetInt32()}"), HttpStatusCode.NoContent);
        var withdrawn = await Read(await Owner.GetAsync($"/api/approvals/races/{id}"));
        Assert.Equal("Withdrawn", withdrawn.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Race_FinalApprovalFailure_DoesNotPublishFalseApproval()
    {
        var raceId = AddRace(capacity: 1);
        var first = await DecideRace(Owner, await Submit(await Proposal(raceId)));
        var second = await DecideRace(Owner, await Submit(await Proposal(raceId)));
        await DecideRace(Manager, first);
        await DecideRace(Manager, second, code: HttpStatusCode.Conflict);
        var current = await Read(await Manager.GetAsync($"/api/approvals/races/{second.GetProperty("id").GetInt32()}"));
        Assert.Equal("PendingManager", current.GetProperty("status").GetString());
        Assert.Equal(1, f.WithDb(db => db.RaceRegistrations.Count(r => r.RaceId == raceId)));
    }

    [Fact]
    public async Task Race_SameHorseSameDay_IsRejected_AndWithdrawalReleasesIt()
    {
        var horseId = f.AddHorse("Duplicate day");
        var raceId = AddRace();
        var proposal = await Proposal(raceId, horseId);
        await Read(await f.Trainer().PostAsJsonAsync("/api/approvals/races", new { horseId, raceId = AddRace() }), HttpStatusCode.Conflict);
        await Read(await f.Trainer().PostAsync($"/api/approvals/races/{proposal.GetProperty("id").GetInt32()}/withdraw", null));
        await Proposal(raceId, horseId);
        await Read(await f.Trainer().PostAsJsonAsync("/api/approvals/races", new { horseId = f.AddHorse("Expired"), raceId = AddRace(2) }), HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Race_RejectionRequiresReason_AndCreatesNoEntry()
    {
        var raceId = AddRace();
        var sent = await Submit(await Proposal(raceId));
        await DecideRace(Owner, sent, false, "", HttpStatusCode.BadRequest);
        var rejected = await DecideRace(Owner, sent, false, "Chưa phù hợp");
        Assert.Equal("Rejected", rejected.GetProperty("status").GetString());
        Assert.Equal("Owner", rejected.GetProperty("rejectedBy").GetString());
        Assert.False(f.WithDb(db => db.RaceRegistrations.Any(r => r.RaceId == raceId)));
    }

    [Theory]
    [InlineData("/api/approvals/plans")]
    [InlineData("/api/approvals/races")]
    public async Task ApprovalEndpoints_RequireAuthenticationAndRole(string path)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await f.Anonymous().GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await f.Groom().GetAsync(path)).StatusCode);
    }
}
