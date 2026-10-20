using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SWP.Application.DTOs.Medical;
using SWP.Domain.Models.Entity;
using SWP.Tests.Support;

namespace SWP.Tests.Api;

public class OriginalCareApiTests(OriginalCareApiFactory factory) : IClassFixture<OriginalCareApiFactory>
{
    [Fact]
    public void ModelUsesOnlyTablesAndColumnsFromOriginalSql()
    {
        _ = factory.Ids;
        var sql = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "OriginalDatabase.sql"));
        var tables = Regex.Matches(sql, @"CREATE TABLE\s+(\w+)\s*\((.*?)\);", RegexOptions.Singleline | RegexOptions.IgnoreCase)
            .ToDictionary(m => m.Groups[1].Value, m => Regex.Matches(m.Groups[2].Value,
                @"^\s*\[?(\w+)\]?\s+(?:BIGINT|INT|NVARCHAR|VARCHAR|DATE|DATETIME|DECIMAL|TIME)\b", RegexOptions.Multiline | RegexOptions.IgnoreCase)
                .Select(c => c.Groups[1].Value).ToHashSet(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);
        factory.WithDb(db =>
        {
            foreach (var entity in db.Model.GetEntityTypes())
            {
                var table = entity.GetTableName()!;
                Assert.True(tables.ContainsKey(table), $"Table {table} is not in the original SQL.");
                var store = StoreObjectIdentifier.Table(table, entity.GetSchema());
                foreach (var property in entity.GetProperties())
                    Assert.Contains(property.GetColumnName(store)!, tables[table]);
            }
            return true;
        });
    }

    [Theory]
    [InlineData("tasks")]
    [InlineData("stable-assignments")]
    [InlineData("stable-numbers")]
    [InlineData("feeding-plans")]
    [InlineData("supplies")]
    [InlineData("incident-reports")]
    public async Task CareReadsRequireGroomRole(string route)
    {
        using var anonymous = factory.Anonymous();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"/api/care/{route}")).StatusCode);
        using var owner = factory.ClientAs(factory.Ids.OwnerId, "Horse Owner");
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.GetAsync($"/api/care/{route}")).StatusCode);
        using var groom = factory.Groom();
        Assert.Equal(HttpStatusCode.OK, (await groom.GetAsync($"/api/care/{route}")).StatusCode);
    }

    [Fact]
    public async Task CareTaskUsesFrontendFields_AndCannotBeCompletedByAnotherGroomOrTwice()
    {
        var horseId = factory.AddHorse("Original care horse");
        var taskId = factory.WithDb(db =>
        {
            var task = new CareTask { HorseId = horseId, UserId = factory.Ids.GroomId,
                TaskName = "Cho ăn buổi sáng", Status = "Pending", ScheduledAt = TestData.Now.DateTime, CreatedAt = TestData.Now.UtcDateTime };
            db.CareTasks.Add(task); db.SaveChanges(); return task.CareTaskId;
        });
        using var groom = factory.Groom();
        var items = (await groom.GetFromJsonAsync<JsonElement>("/api/care/tasks")).EnumerateArray();
        var item = items.Single(x => x.GetProperty("TaskID").GetInt32() == taskId);
        Assert.Equal(horseId, item.GetProperty("HorseID").GetInt32());
        Assert.Equal("Cho ăn buổi sáng", item.GetProperty("LoaiViec").GetString());
        Assert.False(item.GetProperty("TrangThai").GetBoolean());
        Assert.Equal("2026-10-12", item.GetProperty("Ngay").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, (await groom.GetAsync($"/api/care/tasks?groomId={factory.Ids.PendingGroomId}")).StatusCode);

        using var other = factory.ClientAs(factory.Ids.PendingGroomId, "Groom");
        Assert.Equal(HttpStatusCode.Forbidden, (await other.PostAsJsonAsync($"/api/care/tasks/{taskId}/complete", new { GroomID = factory.Ids.GroomId })).StatusCode);
        var done = await groom.PostAsJsonAsync($"/api/care/tasks/{taskId}/complete", new { });
        Assert.Equal(HttpStatusCode.OK, done.StatusCode);
        Assert.True((await done.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("TrangThai").GetBoolean());
        Assert.Equal(HttpStatusCode.Conflict, (await groom.PostAsJsonAsync($"/api/care/tasks/{taskId}/complete", new { })).StatusCode);
        Assert.NotNull(factory.WithDb(db => db.CareTasks.Single(t => t.CareTaskId == taskId).CompletedAt));
    }

    [Fact]
    public async Task IncidentUsesAuthenticatedGroomAndExistingAuditTable_AndSurvivesNewRequest()
    {
        var horseId = factory.AddHorse("Incident horse");
        using var groom = factory.Groom();
        var response = await groom.PostAsJsonAsync("/api/care/incident-reports", new
        { HorseID = horseId, GroomID = factory.Ids.VetId, LoaiSuCo = "Bỏ ăn", MoTa = "  Bỏ bữa sáng  ", Ngay = "2026-10-12" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<JsonElement>();
        var id = report.GetProperty("ReportID").GetInt64();
        Assert.Equal(factory.Ids.GroomId, report.GetProperty("GroomID").GetInt32());
        Assert.Equal("Bỏ bữa sáng", report.GetProperty("MoTa").GetString());
        Assert.True(factory.WithDb(db => db.AuditLogs.Any(a => a.LogId == id && a.RecordId == horseId && a.Action == "ReportIncident")));
        using var second = factory.Groom();
        Assert.Contains((await second.GetFromJsonAsync<JsonElement>("/api/care/incident-reports")).EnumerateArray(), r => r.GetProperty("ReportID").GetInt64() == id);
        using var other = factory.ClientAs(factory.Ids.PendingGroomId, "Groom");
        Assert.DoesNotContain((await other.GetFromJsonAsync<JsonElement>("/api/care/incident-reports")).EnumerateArray(), r => r.GetProperty("ReportID").GetInt64() == id);
    }

    [Theory]
    [InlineData("invalid", "2026-10-12", "")]
    [InlineData("Bỏ ăn", "2999-01-01", "")]
    [InlineData("Bỏ ăn", "2026-10-12", "javascript:alert(1)")]
    [InlineData("Bỏ ăn", "2026-10-12", "data:image/png;base64,abcd")]
    public async Task InvalidIncidentDoesNotWrite(string type, string date, string photo)
    {
        var horse = factory.AddHorse("Invalid incident");
        using var groom = factory.Groom();
        var response = await groom.PostAsJsonAsync("/api/care/incident-reports", new { HorseID = horse, LoaiSuCo = type, Ngay = date, HinhAnh = photo });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(factory.WithDb(db => db.AuditLogs.Any(a => a.Action == "ReportIncident" && a.RecordId == horse)));
    }

    [Fact]
    public async Task ExamLockReleaseAndVaccinationWorkWithoutExtendedColumns()
    {
        var horseId = factory.AddHorse("Original medical horse");
        using var vet = factory.ClientAs(factory.Ids.VetId, "Veterinarian");
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var response = await vet.PostAsJsonAsync("/api/medical/records", new
        {
            HorseID = horseId, Ngay = today, ChanDoan = "Chấn thương chân trước", PhacDo = "Nghỉ ngơi và theo dõi hằng ngày",
            injuries = new[] { "Chân trước trái" }, TrangThai = "Đang điều trị", VetID = factory.Ids.OwnerId
        });
        Assert.True(response.StatusCode == HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var record = result.GetProperty("record");
        var recordId = record.GetProperty("RecordID").GetInt32();
        Assert.Equal(factory.Ids.VetId, record.GetProperty("VetID").GetInt32());
        Assert.True(await vet.GetFromJsonAsync<bool>($"/api/medical/records/{recordId}/is-latest"));
        var lockId = result.GetProperty("lock").GetProperty("LockID").GetInt32();
        var locks = await vet.GetFromJsonAsync<JsonElement>("/api/medical/training-locks/active");
        Assert.Contains(locks.EnumerateArray(), l => l.GetProperty("LockID").GetInt32() == lockId);

        var blocked = await vet.PutAsJsonAsync($"/api/medical/horses/{horseId}/health", new { TrangThai = "Tốt", Ngay = today });
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        var released = await vet.PostAsJsonAsync($"/api/medical/training-locks/{lockId}/release", new { newStatus = "Tốt" });
        Assert.True(released.IsSuccessStatusCode, await released.Content.ReadAsStringAsync());
        Assert.NotNull(factory.WithDb(db => db.TrainingLocks.Single(l => l.LockId == lockId).NgayGo));
        Assert.True(factory.WithDb(db => db.AuditLogs.Any(a => a.Action == "ReleaseTrainingLock" && a.RecordId == lockId && a.UserId == factory.Ids.VetId)));

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

    [Fact]
    public async Task FeedingAndStableReadOriginalDataWithoutReplacingLunchOrUnits()
    {
        var horseId = factory.AddHorse("Feeding horse");
        factory.WithDb(db =>
        {
            var stable = new Stable { StableName = "B12", Status = "Active", CreatedAt = TestData.Now.UtcDateTime };
            db.StableAssignments.Add(new StableAssignment { HorseId = horseId, Stable = stable, Status = "Active", StartDate = TestData.Now.DateTime.AddDays(-1) });
            db.CareTasks.Add(new CareTask { HorseId = horseId, UserId = factory.Ids.GroomId, TaskName = "Kiểm tra nước", Status = "Pending", ScheduledAt = TestData.Today.ToDateTime(new TimeOnly(6, 0)), CreatedAt = TestData.Now.UtcDateTime });
            db.FeedingPlans.Add(new FeedingPlan
            {
                HorseId = horseId, UserId = factory.Ids.VetId, PlanName = "Original diet", StartDate = TestData.Today, Status = "Active", CreatedAt = TestData.Now.UtcDateTime,
                FeedingPlanDetails = [
                    new() { MealName = "Lunch", FeedName = "Quality hay", Quantity = 1500m, Unit = "g", CreatedAt = TestData.Now.UtcDateTime },
                    new() { MealName = "Lunch", FeedName = "Vitamin", Quantity = 0.02m, Unit = "kg", CreatedAt = TestData.Now.UtcDateTime }
                ]
            });
            db.SaveChanges(); return true;
        });
        using var groom = factory.Groom();
        var meals = await groom.GetFromJsonAsync<JsonElement>("/api/care/feeding-plans");
        var meal = meals.EnumerateArray().Single(m => m.GetProperty("HorseID").GetInt32() == horseId);
        Assert.Equal("Trưa", meal.GetProperty("Bua").GetString());
        Assert.Equal(1.5m, meal.GetProperty("Co").GetDecimal());
        Assert.Equal(20m, meal.GetProperty("Vitamin").GetDecimal());
        Assert.Equal(2, meal.GetProperty("ChiTiet").GetArrayLength());
        var stables = await groom.GetFromJsonAsync<JsonElement>("/api/care/stable-assignments");
        var stableDto = stables.EnumerateArray().Single(s => s.GetProperty("HorseID").GetInt32() == horseId);
        Assert.Equal("B12", stableDto.GetProperty("SoChuong").GetString());
        Assert.Equal("06:00 Kiểm tra nước", stableDto.GetProperty("LichSinhHoat").GetString());
    }
}
