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

    public static IEnumerable<object[]> Routes => new[]
    {
        ("GET", "records"), ("GET", "records/1"), ("GET", "records/latest-exam/1"),
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
            HorseID = horseId, LyDo = "Khóa để kiểm tra chấn thương", NgayDat = DateTime.Today.ToString("yyyy-MM-dd"),
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
            HorseID = horseId, Ngay = today, ChanDoan = "Chấn thương chân trước", PhacDo = "Nghỉ ngơi và theo dõi hằng ngày",
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

        var vaccine = await vet.PostAsJsonAsync("/api/medical/vaccinations", new { HorseID = horseId, Loai = "Cúm ngựa", NgayHen = today });
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
