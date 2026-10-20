using System.Net;
using System.Net.Http.Json;
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
}
