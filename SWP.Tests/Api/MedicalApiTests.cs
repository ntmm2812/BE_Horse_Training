using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SWP.Application.DTOs.Medical;
using SWP.Tests.Support;

namespace SWP.Tests.Api;

public class MedicalApiTests : IClassFixture<TrainingApiFactory>
{
    private readonly TrainingApiFactory _factory;
    public MedicalApiTests(TrainingApiFactory factory) => _factory = factory;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SharedOverview_PreservesFrontendDateTimeAndIdFields(bool missingTime)
    {
        var horseId = _factory.AddHorse("Overview calendar contract");
        var day = new DateTime(2026, 10, 21);
        _factory.WithDb(db =>
        {
            db.TrainingSchedules.Add(new SWP.Domain.Models.Entity.TrainingSchedule
            {
                Plan = new SWP.Domain.Models.Entity.TrainingPlan
                {
                    HorseId = horseId, PlanName = "Calendar plan", PlanStatus = "Draft",
                    StartDate = DateOnly.FromDateTime(day)
                },
                TrainingDate = day, StartTime = missingTime ? null : day.AddHours(6).AddMinutes(30),
                Status = "Scheduled", MaxHeartRate = 120m, SessionTitle = "Morning session"
            });
            db.RaceRegistrations.Add(new SWP.Domain.Models.Entity.RaceRegistration
            {
                HorseId = horseId, RegistrationStatus = "Registered",
                Race = new SWP.Domain.Models.Entity.Race
                { RaceName = "Calendar race", Status = "Open", RaceDate = missingTime ? null : day }
            });
            return db.SaveChanges();
        });
        using var vet = _factory.ClientAs(_factory.Ids.VetId, "Veterinarian");
        var response = await vet.GetAsync($"/api/horses/medical-overview?horseId={horseId}");
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        var session = Assert.Single(data.GetProperty("sessions").EnumerateArray());
        Assert.Equal(horseId, session.GetProperty("HorseID").GetInt32());
        Assert.Equal("2026-10-21", session.GetProperty("Ngay").GetString());
        Assert.Equal(missingTime ? "" : "06:30", session.GetProperty("Gio").GetString());
        Assert.Equal(120m, session.GetProperty("NhipTim").GetDecimal());
        var race = Assert.Single(data.GetProperty("races").EnumerateArray());
        Assert.Equal(missingTime ? "" : "2026-10-21", race.GetProperty("Ngay").GetString());
    }

    [Fact]
    public async Task SharedOverview_ScopesOwnerAndExcludesClinicalNotes()
    {
        var owned = _factory.AddHorse("Overview owned horse");
        var other = _factory.AddHorse("Overview other horse");
        _factory.WithDb(db =>
        {
            db.Horses.Single(h => h.HorseId == other).UserId = _factory.Ids.ManagerId;
            foreach (var id in new[] { owned, other })
            {
                db.TrainingLocks.Add(new SWP.Domain.Models.Entity.TrainingLock
                {
                    HorseId = id, VetId = _factory.Ids.VetId, LyDo = "Private clinical reason",
                    NgayDat = DateOnly.FromDateTime(DateTime.Today)
                });
                db.MedicalRecords.Add(new SWP.Domain.Models.Entity.MedicalRecord
                {
                    HorseId = id, CreateBy = _factory.Ids.VetId, RecordDate = DateTime.Today,
                    Diagnosis = "Private diagnosis", Treatment = "Private treatment"
                });
            }
            return db.SaveChanges();
        });

        using var owner = _factory.ClientAs(_factory.Ids.OwnerId, "Horse Owner");
        var response = await owner.GetAsync("/api/horses/medical-overview");
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        var locks = data.GetProperty("locks").EnumerateArray().ToArray();
        Assert.Contains(locks, l => l.GetProperty("HorseID").GetInt32() == owned);
        Assert.DoesNotContain(locks, l => l.GetProperty("HorseID").GetInt32() == other);
        Assert.All(locks, l => Assert.Equal(new[] { "HorseID", "LockID", "NgayDat", "NgayGo" },
            l.EnumerateObject().Select(p => p.Name).OrderBy(n => n).ToArray()));
        var exam = data.GetProperty("latestExams").EnumerateArray().Single(e => e.GetProperty("HorseID").GetInt32() == owned);
        Assert.Equal(DateTime.Today.ToString("yyyy-MM-dd"), exam.GetProperty("Ngay").GetString());
        Assert.DoesNotContain("Private", await response.Content.ReadAsStringAsync());
        var deniedHorse = await owner.GetFromJsonAsync<JsonElement>($"/api/horses/medical-overview?horseId={other}");
        Assert.Empty(deniedHorse.GetProperty("locks").EnumerateArray());
        Assert.Empty(deniedHorse.GetProperty("latestExams").EnumerateArray());
        Assert.Empty(deniedHorse.GetProperty("usedHorseIds").EnumerateArray());

        foreach (var role in new[] { "Veterinarian", "Head Trainer", "Club Manager" })
        {
            using var staff = _factory.ClientAs(_factory.Ids.VetId, role);
            var result = await staff.GetFromJsonAsync<JsonElement>($"/api/horses/medical-overview?horseId={other}");
            Assert.Single(result.GetProperty("locks").EnumerateArray());
        }
        using var groom = _factory.Groom();
        using var anonymous = _factory.Anonymous();
        Assert.Equal(HttpStatusCode.Forbidden, (await groom.GetAsync("/api/horses/medical-overview")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/horses/medical-overview")).StatusCode);
    }

    public static IEnumerable<object[]> Routes => new[]
    {
        ("GET", "incident-reports"), ("GET", "records"), ("GET", "records/1"), ("GET", "records/latest-exam/1"),
        ("POST", "records"), ("PUT", "records/1"),
        ("GET", "training-locks"), ("GET", "training-locks/active"),
        ("POST", "training-locks"), ("POST", "training-locks/1/release"),
        ("GET", "vaccinations"), ("GET", "vaccinations/1"),
        ("POST", "vaccinations"), ("PUT", "vaccinations/1"),
        ("DELETE", "vaccinations/1"), ("POST", "vaccinations/1/mark-done"), ("GET", "vets")
    }.Select(x => new object[] { x.Item1, x.Item2 });

    [Theory]
    [MemberData(nameof(Routes))]
    public async Task MedicalEndpoints_RejectAnonymousAndOtherRoles(string method, string route)
    {
        using var anonymous = _factory.Anonymous();
        using var request = new HttpRequestMessage(new HttpMethod(method), $"/api/medical/{route}") { Content = JsonContent.Create(new { }) };
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.SendAsync(request)).StatusCode);

        foreach (var role in new[] { "Head Trainer", "Groom", "Horse Owner", "Club Manager" })
        {
            using var client = _factory.ClientAs(_factory.Ids.OwnerId, role);
            using var denied = new HttpRequestMessage(new HttpMethod(method), $"/api/medical/{route}") { Content = JsonContent.Create(new { }) };
            Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(denied)).StatusCode);
        }
    }

    [Fact]
    public async Task VetCanCreateLock_IdentityComesFromJwt_AndReleaseAcceptsFrontendRequest()
    {
        var horseId = _factory.AddHorse("API medical lock");
        using var client = _factory.ClientAs(_factory.Ids.VetId, "Veterinarian");
        var response = await client.PostAsJsonAsync("/api/medical/training-locks", new
        {
            HorseID = horseId.ToString(), LyDo = "Khóa để kiểm tra chấn thương", NgayDat = DateTime.Today.ToString("yyyy-MM-dd"),
            VetID = _factory.Ids.OwnerId
        });
        Assert.True(response.StatusCode == HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var created = (await response.Content.ReadFromJsonAsync<TrainingLockDto>())!;
        Assert.Equal(_factory.Ids.VetId, created.VetID);

        // Lý do có nhập nhưng quá ngắn thì vẫn bị từ chối, khóa giữ nguyên.
        var rejected = await client.PostAsJsonAsync($"/api/medical/training-locks/{created.LockID}/release",
            new { NewStatus = "Tốt", ReleaseReason = "ab" });
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.Null(_factory.WithDb(db => db.TrainingLocks.Single(l => l.LockId == created.LockID).NgayGo));

        // FE chỉ gửi newStatus: mở khóa thành công, người mở lấy từ JWT.
        var released = await client.PostAsJsonAsync($"/api/medical/training-locks/{created.LockID}/release", new { NewStatus = "Tốt" });
        Assert.True(released.StatusCode == HttpStatusCode.OK, await released.Content.ReadAsStringAsync());
        var stored = _factory.WithDb(db => db.TrainingLocks.Single(l => l.LockId == created.LockID));
        Assert.NotNull(stored.NgayGo);
        var audit = _factory.WithDb(db => db.AuditLogs.Single(a => a.Action == "ReleaseTrainingLock" && a.RecordId == created.LockID));
        Assert.Equal(_factory.Ids.VetId, audit.UserId);
    }

    [Fact]
    public async Task ExamLockReleaseAndVaccinationUseSharedApplicationContext()
    {
        var horseId = _factory.AddHorse("Original medical horse");
        using var vet = _factory.ClientAs(_factory.Ids.VetId, "Veterinarian");
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var response = await vet.PostAsJsonAsync("/api/medical/records", new
        {
            HorseID = horseId.ToString(), Ngay = today, ChanDoan = "Chấn thương chân trước", PhacDo = "Nghỉ ngơi và theo dõi hằng ngày",
            injuries = new[] { "Chân trước trái" }, TrangThai = "Đang điều trị", VetID = _factory.Ids.OwnerId
        });
        Assert.True(response.StatusCode == HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var record = result.GetProperty("record");
        var recordId = record.GetProperty("RecordID").GetInt32();
        Assert.Equal(_factory.Ids.VetId, record.GetProperty("VetID").GetInt32());
        Assert.True(await vet.GetFromJsonAsync<bool>($"/api/medical/records/{recordId}/is-latest"));
        var lockId = result.GetProperty("lock").GetProperty("LockID").GetInt32();
        var locks = await vet.GetFromJsonAsync<JsonElement>("/api/medical/training-locks/active");
        Assert.Contains(locks.EnumerateArray(), l => l.GetProperty("LockID").GetInt32() == lockId);

        var blocked = await vet.PutAsJsonAsync($"/api/medical/horses/{horseId}/health", new { TrangThai = "Tốt", Ngay = today });
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        var released = await vet.PostAsJsonAsync($"/api/medical/training-locks/{lockId}/release", new { newStatus = "Tốt" });
        Assert.True(released.IsSuccessStatusCode, await released.Content.ReadAsStringAsync());
        Assert.NotNull(_factory.WithDb(db => db.TrainingLocks.Single(l => l.LockId == lockId).NgayGo));
        Assert.True(_factory.WithDb(db => db.AuditLogs.Any(a => a.Action == "ReleaseTrainingLock" && a.RecordId == lockId && a.UserId == _factory.Ids.VetId)));

        var vaccine = await vet.PostAsJsonAsync("/api/medical/vaccinations", new { HorseID = horseId.ToString(), Loai = "Cúm ngựa", NgayHen = today });
        Assert.Equal(HttpStatusCode.Created, vaccine.StatusCode);
        var scheduleId = (await vaccine.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("ScheduleID").GetInt32();
        var duplicate = await vet.PostAsJsonAsync("/api/medical/vaccinations", new { HorseID = horseId, Loai = "Cúm ngựa", NgayHen = today });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var done = await vet.PostAsJsonAsync($"/api/medical/vaccinations/{scheduleId}/mark-done", new { nextDate = DateTime.Today.AddDays(30).ToString("yyyy-MM-dd") });
        Assert.True(done.IsSuccessStatusCode, await done.Content.ReadAsStringAsync());
        var vaccineResult = await done.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Đã tiêm", vaccineResult.GetProperty("item").GetProperty("TrangThai").GetString());
        Assert.Equal("Chờ tiêm", vaccineResult.GetProperty("next").GetProperty("TrangThai").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await vet.DeleteAsync($"/api/medical/vaccinations/{scheduleId}")).StatusCode);
    }

}
