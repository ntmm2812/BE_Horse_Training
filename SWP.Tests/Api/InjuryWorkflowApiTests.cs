using System.Net;
using System.Net.Http.Json;
using SWP.Application.DTOs.Medical;
using SWP.Tests.Support;

namespace SWP.Tests.Api;

public class InjuryWorkflowApiTests(TrainingApiFactory factory) : IClassFixture<TrainingApiFactory>
{
    private OpenInjuryCaseRequest CaseRequest() => new()
    {
        HorseId = factory.AddHorse("Case API horse"), Title = "Chấn thương chân trước", BodyRegion = "Chân trước",
        ClinicalNotes = "private-clinical-note", OwnerSummary = "Tóm tắt cho chủ ngựa", TrainingGuidance = "Chờ bác sĩ đánh giá"
    };

    public static IEnumerable<object[]> Routes => new[]
    {
        ("GET", ""), ("GET", "/1"), ("GET", "/1/timeline"), ("POST", ""),
        ("PUT", "/1/summary"), ("POST", "/1/notes"), ("POST", "/1/exams"),
        ("POST", "/1/status"), ("POST", "/1/training-locks"), ("POST", "/1/care-tasks"),
        ("GET", "/care-tasks"), ("GET", "/care-overview"), ("GET", "/assignable-grooms"),
        ("POST", "/care-tasks/1/start"), ("POST", "/care-tasks/1/complete"),
        ("POST", "/care-tasks/1/unable"), ("POST", "/care-tasks/1/cancel"), ("PUT", "/care-tasks/1/assignment")
    }.Select(r => new object[] { r.Item1, r.Item2 });

    [Theory]
    [MemberData(nameof(Routes))]
    public async Task AllRoutesRejectAnonymous(string method, string path)
    {
        using var client = factory.Anonymous();
        using var request = new HttpRequestMessage(new HttpMethod(method), "/api/injury-cases" + path) { Content = JsonContent.Create(new { }) };
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task VetAssigns_GroomExecutes_OwnerAndManagerHaveRestrictedViews()
    {
        using var vet = factory.ClientAs(factory.Ids.VetId, "Veterinarian");
        var grooms = (await vet.GetFromJsonAsync<List<AssignableGroomDto>>("/api/injury-cases/assignable-grooms"))!;
        Assert.Contains(grooms, g => g.UserId == factory.Ids.GroomId);
        Assert.DoesNotContain(grooms, g => g.UserId == factory.Ids.PendingGroomId || g.UserId == factory.Ids.VetId);
        var response = await vet.PostAsJsonAsync("/api/injury-cases", CaseRequest());
        Assert.True(response.StatusCode == HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var injury = (await response.Content.ReadFromJsonAsync<InjuryCaseDto>())!;
        Assert.NotNull(response.Headers.Location);
        var taskResponse = await vet.PostAsJsonAsync($"/api/injury-cases/{injury.Id}/care-tasks", new AssignMedicalCareRequest
        {
            Version = injury.Version, GroomId = factory.Ids.GroomId, Title = "Ghi nhận biểu hiện vận động",
            Instructions = "private-care-instruction", DueAt = factory.Clock.GetUtcNow().AddHours(2)
        });
        Assert.Equal(HttpStatusCode.Created, taskResponse.StatusCode);
        var task = (await taskResponse.Content.ReadFromJsonAsync<MedicalCareDto>())!;
        using var groom = factory.Groom();
        var mine = (await groom.GetFromJsonAsync<PageDto<MedicalCareDto>>($"/api/injury-cases/care-tasks?caseId={injury.Id}"))!;
        Assert.Single(mine.Items);
        var start = await groom.PostAsJsonAsync($"/api/injury-cases/care-tasks/{task.Id}/start", new { task.Version });
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        task = (await start.Content.ReadFromJsonAsync<MedicalCareDto>())!;
        var complete = await groom.PostAsJsonAsync($"/api/injury-cases/care-tasks/{task.Id}/complete", new { task.Version, Notes = "Đã ghi nhận đầy đủ theo hướng dẫn" });
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await groom.GetAsync($"/api/injury-cases/{injury.Id}/timeline")).StatusCode);

        using var owner = factory.ClientAs(factory.Ids.OwnerId, "Horse Owner");
        var ownerResponse = await owner.GetAsync($"/api/injury-cases/{injury.Id}");
        Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);
        Assert.DoesNotContain("private-clinical-note", await ownerResponse.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.GetAsync($"/api/injury-cases/care-tasks?caseId={injury.Id}")).StatusCode);
        using var manager = factory.ClientAs(factory.Ids.ManagerId, "Club Manager");
        var operational = await manager.GetAsync($"/api/injury-cases/care-tasks?caseId={injury.Id}");
        Assert.Equal(HttpStatusCode.OK, operational.StatusCode);
        Assert.DoesNotContain("private-care-instruction", await operational.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, (await manager.GetAsync("/api/injury-cases/care-overview")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PostAsJsonAsync($"/api/injury-cases/{injury.Id}/status", new { })).StatusCode);
    }

    [Theory]
    [InlineData("Head Trainer")]
    [InlineData("Groom")]
    [InlineData("Horse Owner")]
    [InlineData("Club Manager")]
    public async Task NonVetCannotCreateCase(string role)
    {
        using var client = factory.ClientAs(factory.Ids.OwnerId, role);
        var response = await client.PostAsJsonAsync("/api/injury-cases", CaseRequest());
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RoleClaimDoesNotOverrideActualAccountRole()
    {
        using var client = factory.ClientAs(factory.Ids.OwnerId, "Veterinarian");
        var response = await client.PostAsJsonAsync("/api/injury-cases", CaseRequest());
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task StaleVersionReturns409_AndSwaggerIncludesNewRoutes()
    {
        using var vet = factory.ClientAs(factory.Ids.VetId, "Veterinarian");
        var response = await vet.PostAsJsonAsync("/api/injury-cases", CaseRequest());
        var injury = (await response.Content.ReadFromJsonAsync<InjuryCaseDto>())!;
        var note = new { Version = injury.Version, Note = "Bổ sung diễn biến mới" };
        Assert.Equal(HttpStatusCode.OK, (await vet.PostAsJsonAsync($"/api/injury-cases/{injury.Id}/notes", note)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await vet.PostAsJsonAsync($"/api/injury-cases/{injury.Id}/notes", note)).StatusCode);
        var swagger = await vet.GetStringAsync("/swagger/v1/swagger.json");
        Assert.Contains("/api/injury-cases/{id}/care-tasks", swagger);
        Assert.Contains("/api/injury-cases/care-tasks/{taskId}/unable", swagger);
    }
}
