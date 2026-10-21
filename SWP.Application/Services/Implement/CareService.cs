using System.Globalization;
using System.Text.Json;
using SWP.Application.DTOs.Care;
using SWP.Application.Services.Interface;
using SWP.Domain.Models.Entity;
using SWP.Infrastructure.Repositories.Interface;

namespace SWP.Application.Services.Implement;

public sealed class CareException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed class CareService(ICareRepository repo, TimeProvider clock) : ICareService
{
    private const string IncidentAction = "ReportIncident";
    private const int MaxImageBytes = 5 * 1024 * 1024;
    private static readonly string[] IncidentTypes = ["Vết thương", "Khập khiễng", "Bỏ ăn", "Chuồng/thiết bị hỏng", "Khác"];
    private DateTime Now => clock.GetUtcNow().ToOffset(TimeSpan.FromHours(7)).DateTime;

    public async Task<List<CareTaskDto>> GetTasks(int groomId, CancellationToken ct)
    {
        var tasks = await repo.GetGroomDailyTasksAsync(groomId, ct);
        return tasks.Select(ToTask).ToList();
    }

    public async Task<CareTaskDto> CompleteTask(int id, int groomId, CancellationToken ct)
    {
        var task = await repo.GetCareTaskByIdAsync(id, ct)
            ?? throw new CareException(404, "Không tìm thấy công việc.");

        if (task.UserId != groomId)
            throw new CareException(403, "Bạn chỉ được xác nhận công việc của chính mình.");

        if (string.Equals(task.TaskType?.Trim(), "Medical", StringComparison.OrdinalIgnoreCase))
            throw new CareException(409, "Công việc y tế không thuộc phạm vi xác nhận công việc hằng ngày.");

        if (task.Status is not ("Pending" or "InProgress" or "Scheduled"))
            throw new CareException(409, "Công việc đã hoàn thành, đã hủy hoặc không thể xác nhận.");

        task.Status = "Completed";
        task.CompletedAt = clock.GetUtcNow().UtcDateTime;
        await repo.SaveChangesAsync(ct);
        return ToTask(task);
    }

    private static CareTaskDto ToTask(CareTask t) => new(
        t.CareTaskId,
        t.HorseId,
        t.UserId,
        t.TaskName,
        t.Status == "Completed",
        (t.ScheduledAt ?? t.CreatedAt).ToString("yyyy-MM-dd"));

    public async Task<List<StableAssignmentDto>> GetStableAssignments(CancellationToken ct)
    {
        var now = Now;
        var assignments = await repo.GetActiveStableAssignmentsAsync(now, ct);
        var today = now.Date;
        var tomorrow = today.AddDays(1);
        var tasks = await repo.GetDailyCareTasksForDateAsync(today, tomorrow, ct);

        return assignments.DistinctBy(a => a.HorseId).Select(a => new StableAssignmentDto(
            a.StableId,
            a.HorseId,
            a.Stable.StableName,
            string.Join("\n", tasks.Where(t => t.HorseId == a.HorseId)
                .Select(t => $"{t.ScheduledAt:HH:mm} {t.TaskName}")))).ToList();
    }

    public async Task<Dictionary<int, string>> GetStableNumbers(CancellationToken ct) =>
        (await GetStableAssignments(ct)).ToDictionary(a => a.HorseID, a => a.SoChuong);

    public async Task<List<FeedingPlanDto>> GetFeedingPlans(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(Now);
        var plans = await repo.GetActiveFeedingPlansAsync(today, ct);

        return plans.DistinctBy(p => p.HorseId).SelectMany(p => p.FeedingPlanDetails
            .GroupBy(d => new { d.MealName, d.FeedingTime })
            .Select(meal => new FeedingPlanDto(
                meal.Min(d => d.FeedingPlanDetailId),
                p.HorseId,
                SumFood(meal, "grain"),
                SumFood(meal, "hay"),
                SumFood(meal, "vitamin"),
                MealLabel(meal.Key.MealName),
                meal.Select(d => new FeedDetailDto(d.FeedName, d.Quantity, d.Unit, d.Instructions)).ToList())))
            .OrderBy(p => p.HorseID)
            .ThenBy(p => p.FeedID)
            .ToList();
    }

    private static decimal SumFood(IEnumerable<FeedingPlanDetail> meal, string category) => meal.Sum(d =>
    {
        var name = (d.FeedName ?? "").ToLowerInvariant();
        var matches = category switch
        {
            "hay" => name.Contains("hay") || name.Contains("grass") || name.Contains("cỏ"),
            "grain" => name.Contains("grain") || name.Contains("oat") || name.Contains("ngũ cốc") || name.Contains("pellet"),
            _ => name.Contains("vitamin")
        };
        if (!matches) return 0m;
        var kg = d.Unit?.Trim().ToLowerInvariant() switch { "kg" => d.Quantity, "g" => d.Quantity / 1000m, _ => 0m };
        return category == "vitamin" ? kg * 1000m : kg;
    });

    private static string MealLabel(string? meal) => meal?.Trim().ToLowerInvariant() switch
    {
        "breakfast" or "morning" or "sáng" => "Sáng",
        "afternoon" or "chiều" => "Chiều",
        "dinner" or "evening" or "tối" => "Tối",
        "lunch" or "trưa" => "Trưa",
        _ => meal ?? ""
    };

    public async Task<List<CareSupplyDto>> GetSupplies(CancellationToken ct)
    {
        var supplies = await repo.GetActiveSuppliesAsync(ct);
        return supplies.Select(s => new CareSupplyDto(
            s.SupplyId,
            s.SupplyName,
            s.Category,
            s.QuantityInStock,
            s.UserId,
            s.UnitPrice,
            s.Unit)).ToList();
    }

    public Task<List<IncidentReportDto>> GetIncidents(int groomId, CancellationToken ct) =>
        ReadIncidents(groomId, null, ct);

    public Task<List<IncidentReportDto>> GetIncidentsForVet(int? horseId, CancellationToken ct) =>
        ReadIncidents(null, horseId, ct);

    private async Task<List<IncidentReportDto>> ReadIncidents(int? groomId, int? horseId, CancellationToken ct)
    {
        var reports = await repo.GetIncidentAuditLogsAsync(groomId, horseId, ct);
        return reports.Select(a =>
        {
            var data = JsonSerializer.Deserialize<CreateIncidentReportRequest>(a.NewValue ?? "{}")!;
            return new IncidentReportDto(
                a.LogId,
                a.RecordId ?? data.HorseID,
                a.UserId!.Value,
                data.LoaiSuCo,
                data.MoTa ?? "",
                data.HinhAnh ?? "",
                data.Ngay);
        }).OrderByDescending(r => r.Ngay).ThenByDescending(r => r.ReportID).ToList();
    }

    public async Task<IncidentReportDto> CreateIncident(CreateIncidentReportRequest request, int groomId, CancellationToken ct)
    {
        if (!IncidentTypes.Contains(request.LoaiSuCo))
            throw new CareException(400, "Loại sự cố không hợp lệ.");

        var description = request.MoTa?.Trim() ?? "";
        if (description.Length > 300)
            throw new CareException(400, "Mô tả tối đa 300 ký tự.");

        if (!DateOnly.TryParseExact(request.Ngay, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            || date > DateOnly.FromDateTime(Now))
            throw new CareException(400, "Ngày không hợp lệ hoặc ở tương lai.");

        var photo = request.HinhAnh?.Trim() ?? "";
        ValidatePhoto(photo);

        if (!await repo.HorseExistsAsync(request.HorseID, ct))
            throw new CareException(404, "Không tìm thấy ngựa.");

        var data = new CreateIncidentReportRequest
        {
            HorseID = request.HorseID,
            LoaiSuCo = request.LoaiSuCo,
            MoTa = description,
            HinhAnh = photo,
            Ngay = request.Ngay
        };

        var audit = new AuditLog
        {
            Action = IncidentAction,
            TableName = "Horse",
            RecordId = request.HorseID,
            UserId = groomId,
            CreatedAt = clock.GetUtcNow().UtcDateTime,
            NewValue = JsonSerializer.Serialize(data)
        };

        await repo.AddIncidentAuditLogAsync(audit, ct);
        await repo.SaveChangesAsync(ct);

        return new(audit.LogId, request.HorseID, groomId, request.LoaiSuCo, description, photo, request.Ngay);
    }

    private static void ValidatePhoto(string photo)
    {
        if (photo.Length == 0) return;
        var prefix = photo.StartsWith("data:image/png;base64,", StringComparison.Ordinal) ? "data:image/png;base64,"
            : photo.StartsWith("data:image/jpeg;base64,", StringComparison.Ordinal) ? "data:image/jpeg;base64," : null;
        if (prefix is not null)
        {
            if (photo.Length - prefix.Length > ((MaxImageBytes + 2) / 3) * 4)
                throw new CareException(400, "Ảnh tối đa 5MB.");
            byte[] bytes;
            try { bytes = Convert.FromBase64String(photo[prefix.Length..]); }
            catch (FormatException) { throw new CareException(400, "Dữ liệu ảnh không hợp lệ."); }
            var valid = prefix.Contains("png")
                ? bytes.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
                : bytes.AsSpan().StartsWith(new byte[] { 255, 216, 255 });
            if (!valid || bytes.Length > MaxImageBytes) throw new CareException(400, "Ảnh phải là JPG/PNG và tối đa 5MB.");
            return;
        }
        if (photo.Length > 2000 || !Uri.TryCreate(photo, UriKind.Absolute, out var uri) || uri.Scheme is not ("https" or "http"))
            throw new CareException(400, "Ảnh phải là URL HTTP(S) hoặc dữ liệu JPG/PNG.");
    }
}
