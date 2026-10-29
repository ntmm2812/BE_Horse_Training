using System.Globalization;
using System.Text;
using SWP.Application.Common;
using SWP.Application.DTOs.Care;
using SWP.Application.Services.Interface;
using SWP.Domain.Common;
using SWP.Domain.Models.Entity;
using SWP.Infrastructure.Repositories.Interface;

namespace SWP.Application.Services.Implement;

public class CareService : ICareService
{
    private static readonly string[] ClosedStableStatuses = ["Ended", "Inactive", "Cancelled", "Closed"];
    private static readonly string[] StaffRoles = [RoleNames.HeadTrainer, RoleNames.Veterinarian, RoleNames.Groom];
    private static readonly string[] MealOrder = ["Sáng", "Chiều", "Tối"];

    private static readonly (string Name, TimeOnly Time)[] DailyRoutine =
    [
        ("Cho ăn buổi sáng", new TimeOnly(6, 0)),
        ("Dọn chuồng", new TimeOnly(8, 0)),
        ("Tắm và chải lông", new TimeOnly(10, 0)),
        ("Kiểm tra móng", new TimeOnly(14, 0)),
        ("Vệ sinh yên cương", new TimeOnly(15, 30)),
        ("Cho ăn buổi chiều", new TimeOnly(17, 0)),
    ];

    private readonly ICareRepository _care;
    private readonly TimeProvider _clock;

    public CareService(ICareRepository care, TimeProvider clock)
    {
        _care = care;
        _clock = clock;
    }

    public async Task<IReadOnlyList<CareTaskDto>> GetTasksAsync(int groomId, CancellationToken cancellationToken = default)
    {
        var tasks = await _care.ListTasksAsync(groomId, cancellationToken);
        if (await EnsureTodayRoutineAsync(groomId, tasks, cancellationToken))
        {
            tasks = await _care.ListTasksAsync(groomId, cancellationToken);
        }

        return tasks.Select(MapTask).ToList();
    }

    private async Task<bool> EnsureTodayRoutineAsync(
        int groomId,
        IReadOnlyList<CareTask> existing,
        CancellationToken cancellationToken)
    {
        var horseIds = existing.Select(t => t.HorseId).Distinct().ToList();
        if (horseIds.Count == 0)
        {
            return false;
        }

        var today = LocalToday();
        var covered = existing
            .Where(t => LocalDate(t.ScheduledAt ?? t.CreatedAt) == today)
            .Select(t => t.HorseId)
            .ToHashSet();
        var now = _clock.GetUtcNow().UtcDateTime;
        var added = false;
        foreach (var horseId in horseIds.Where(id => !covered.Contains(id)))
        {
            foreach (var (name, time) in DailyRoutine)
            {
                _care.AddTask(new CareTask
                {
                    HorseId = horseId,
                    UserId = groomId,
                    TaskName = name,
                    Status = CareTaskStatuses.Pending,
                    ScheduledAt = today.ToDateTime(time),
                    CreatedAt = now,
                });
                added = true;
            }
        }

        if (added)
        {
            await _care.SaveAsync(cancellationToken);
        }

        return added;
    }

    public async Task<ServiceResult<CareTaskDto>> CompleteTaskAsync(
        int taskId,
        int groomId,
        CancellationToken cancellationToken = default)
    {
        var task = await _care.GetTaskTrackedAsync(taskId, cancellationToken);
        if (task is null)
        {
            return ServiceResult<CareTaskDto>.Fail(ServiceErrorCodes.NotFound, "Không tìm thấy công việc.");
        }

        if (task.UserId != groomId)
        {
            return ServiceResult<CareTaskDto>.Fail(ServiceErrorCodes.Conflict, "Bạn chỉ được xác nhận công việc của chính mình.");
        }

        if (CareTaskStatuses.IsCompleted(task.Status, task.CompletedAt))
        {
            return ServiceResult<CareTaskDto>.Fail(ServiceErrorCodes.Conflict, "Công việc này đã được xác nhận.");
        }

        task.Status = CareTaskStatuses.Completed;
        task.CompletedAt = _clock.GetUtcNow().UtcDateTime;
        await _care.SaveAsync(cancellationToken);
        return ServiceResult<CareTaskDto>.Ok(MapTask(task));
    }

    public async Task<IReadOnlyList<StableAssignmentDto>> GetStablesAsync(CancellationToken cancellationToken = default)
    {
        var today = LocalToday();
        var start = today.ToDateTime(TimeOnly.MinValue);
        var assignments = await _care.ListOpenAssignmentsAsync(start, cancellationToken);
        var tasks = await _care.ListScheduledBetweenAsync(start, start.AddDays(1), cancellationToken);
        var tasksByHorse = tasks.GroupBy(t => t.HorseId).ToDictionary(g => g.Key, g => g.ToList());

        return assignments
            .Where(a => !IsClosedStable(a.Status))
            .GroupBy(a => a.HorseId)
            .Select(group => group
                .OrderByDescending(a => a.EndDate == null)
                .ThenByDescending(a => a.StartDate)
                .First())
            .Select(a => new StableAssignmentDto
            {
                StableID = a.StableId,
                HorseID = a.HorseId,
                SoChuong = a.Stable.StableName,
                LichSinhHoat = ScheduleText(a, tasksByHorse.GetValueOrDefault(a.HorseId)),
            })
            .OrderBy(a => a.SoChuong, StringComparer.CurrentCulture)
            .ToList();
    }

    public async Task<IReadOnlyList<FeedingMealDto>> GetFeedingAsync(CancellationToken cancellationToken = default)
    {
        var plans = await _care.ListPlansInRangeAsync(LocalToday(), cancellationToken);
        var current = plans
            .Where(p => FeedingPlanStatuses.IsCurrent(p.Status))
            .GroupBy(p => p.HorseId)
            .Select(group => group.OrderByDescending(p => p.StartDate).First());

        var meals = new List<FeedingMealDto>();
        foreach (var plan in current)
        {
            var groups = plan.FeedingPlanDetails
                .GroupBy(d => NormalizeMeal(d.MealName, d.FeedingTime))
                .OrderBy(g => MealRank(g.Key));

            foreach (var group in groups)
            {
                decimal grain = 0;
                decimal hay = 0;
                decimal vitamin = 0;
                foreach (var detail in group)
                {
                    AddComponent(detail.FeedName, detail.Quantity, ref grain, ref hay, ref vitamin);
                }

                meals.Add(new FeedingMealDto
                {
                    FeedID = group.Min(d => d.FeedingPlanDetailId),
                    HorseID = plan.HorseId,
                    NguCoc = grain,
                    Co = hay,
                    Vitamin = vitamin,
                    Bua = group.Key,
                });
            }
        }

        return meals;
    }

    public async Task<IReadOnlyList<IncidentReportDto>> GetIncidentsAsync(int groomId, CancellationToken cancellationToken = default)
    {
        var reports = await _care.ListIncidentsAsync(groomId, cancellationToken);
        return reports.Select(MapIncident).ToList();
    }

    public async Task<ServiceResult<IncidentReportDto>> CreateIncidentAsync(
        CreateIncidentReportRequestDto request,
        int groomId,
        CancellationToken cancellationToken = default)
    {
        if (request.HorseID <= 0 || !await _care.HorseExistsAsync(request.HorseID, cancellationToken))
        {
            return ServiceResult<IncidentReportDto>.Fail(ServiceErrorCodes.Validation, "Vui lòng chọn ngựa.");
        }

        if (!IncidentTypes.TryNormalize(request.LoaiSuCo, out var incidentType))
        {
            return ServiceResult<IncidentReportDto>.Fail(ServiceErrorCodes.Validation, "Loại sự cố không hợp lệ.");
        }

        var description = string.IsNullOrWhiteSpace(request.MoTa) ? null : request.MoTa.Trim();
        if (description is not null && description.Length > IncidentTypes.DescriptionMaxLength)
        {
            return ServiceResult<IncidentReportDto>.Fail(
                ServiceErrorCodes.Validation,
                $"Mô tả tối đa {IncidentTypes.DescriptionMaxLength} ký tự.");
        }

        if (request.Ngay == default)
        {
            return ServiceResult<IncidentReportDto>.Fail(ServiceErrorCodes.Validation, "Vui lòng chọn ngày.");
        }

        if (request.Ngay > LocalToday())
        {
            return ServiceResult<IncidentReportDto>.Fail(ServiceErrorCodes.Validation, "Ngày không được ở tương lai.");
        }

        var imageError = ValidateImage(request.HinhAnh, out var imageUrl);
        if (imageError is not null)
        {
            return ServiceResult<IncidentReportDto>.Fail(ServiceErrorCodes.Validation, imageError);
        }

        var report = new IncidentReport
        {
            HorseId = request.HorseID,
            GroomId = groomId,
            IncidentType = incidentType,
            Description = description,
            ImageUrl = imageUrl,
            ReportedOn = request.Ngay,
            CreatedAt = _clock.GetUtcNow().UtcDateTime,
        };
        _care.AddIncident(report);
        await _care.SaveAsync(cancellationToken);
        return ServiceResult<IncidentReportDto>.Ok(MapIncident(report));
    }

    public async Task<IReadOnlyList<SupplyDto>> GetSuppliesAsync(CancellationToken cancellationToken = default)
    {
        var supplies = await _care.ListSuppliesAsync(cancellationToken);
        return supplies.Select(MapSupply).ToList();
    }

    public async Task<SupplyDto?> GetSupplyAsync(int supplyId, CancellationToken cancellationToken = default)
    {
        var supply = await _care.GetSupplyTrackedAsync(supplyId, cancellationToken);
        return supply is null ? null : MapSupply(supply);
    }

    public async Task<ServiceResult<SupplyDto>> CreateSupplyAsync(
        SaveSupplyRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var parsed = await ParseSupplyAsync(request, currentManagerId: null, cancellationToken);
        if (!parsed.Success)
        {
            return ServiceResult<SupplyDto>.Fail(parsed.ErrorCode!, parsed.ErrorMessage!);
        }

        var (name, category, managerId) = parsed.Data;
        var now = _clock.GetUtcNow().UtcDateTime;
        var supply = new Supply
        {
            SupplyName = name,
            Category = category,
            QuantityInStock = request.SoLuongTon,
            UnitPrice = request.DonGia,
            UserId = managerId,
            Status = "Active",
            CreatedAt = now,
        };
        _care.AddSupply(supply);
        await _care.SaveAsync(cancellationToken);
        return ServiceResult<SupplyDto>.Ok(MapSupply(supply));
    }

    public async Task<ServiceResult<SupplyDto>> UpdateSupplyAsync(
        int supplyId,
        SaveSupplyRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var supply = await _care.GetSupplyTrackedAsync(supplyId, cancellationToken);
        if (supply is null)
        {
            return ServiceResult<SupplyDto>.Fail(ServiceErrorCodes.NotFound, "Không tìm thấy vật tư.");
        }

        var parsed = await ParseSupplyAsync(request, supply.UserId, cancellationToken);
        if (!parsed.Success)
        {
            return ServiceResult<SupplyDto>.Fail(parsed.ErrorCode!, parsed.ErrorMessage!);
        }

        var (name, category, managerId) = parsed.Data;
        supply.SupplyName = name;
        supply.Category = category;
        supply.QuantityInStock = request.SoLuongTon;
        supply.UnitPrice = request.DonGia;
        supply.UserId = managerId;
        supply.UpdatedAt = _clock.GetUtcNow().UtcDateTime;
        await _care.SaveAsync(cancellationToken);
        return ServiceResult<SupplyDto>.Ok(MapSupply(supply));
    }

    public async Task<ServiceResult<SupplyDto>> AdjustStockAsync(
        int supplyId,
        decimal delta,
        CancellationToken cancellationToken = default)
    {
        var supply = await _care.GetSupplyTrackedAsync(supplyId, cancellationToken);
        if (supply is null)
        {
            return ServiceResult<SupplyDto>.Fail(ServiceErrorCodes.NotFound, "Không tìm thấy vật tư.");
        }

        var next = supply.QuantityInStock + delta;
        if (next < 0)
        {
            return ServiceResult<SupplyDto>.Fail(
                ServiceErrorCodes.Conflict,
                $"Chỉ còn {FormatQty(supply.QuantityInStock)} trong kho, không thể xuất {FormatQty(-delta)}.");
        }

        supply.QuantityInStock = next;
        supply.UpdatedAt = _clock.GetUtcNow().UtcDateTime;
        await _care.SaveAsync(cancellationToken);
        return ServiceResult<SupplyDto>.Ok(MapSupply(supply));
    }

    public async Task<ServiceResult<bool>> DeleteSupplyAsync(int supplyId, CancellationToken cancellationToken = default)
    {
        var supply = await _care.GetSupplyTrackedAsync(supplyId, cancellationToken);
        if (supply is null)
        {
            return ServiceResult<bool>.Fail(ServiceErrorCodes.NotFound, "Không tìm thấy vật tư.");
        }

        _care.RemoveSupply(supply);
        await _care.SaveAsync(cancellationToken);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<IReadOnlyList<StaffMemberDto>> GetStaffAsync(CancellationToken cancellationToken = default)
    {
        var users = await _care.ListUsersWithRolesAsync(cancellationToken);
        return users
            .Select(u => new { User = u, RoleId = CanonicalRole(u) })
            .Where(x => x.RoleId is not null && StaffRoles.Contains(x.RoleId))
            .Select(x => new StaffMemberDto
            {
                UserID = x.User.UserId,
                HoTen = x.User.FullName,
                RoleId = x.RoleId!,
                IsActive = string.Equals(x.User.Status, UserStatuses.Active, StringComparison.OrdinalIgnoreCase),
            })
            .ToList();
    }

    private async Task<ServiceResult<(string Name, string Category, int ManagerId)>> ParseSupplyAsync(
        SaveSupplyRequestDto request,
        int? currentManagerId,
        CancellationToken cancellationToken)
    {
        var name = (request.TenVatTu ?? string.Empty).Trim();
        if (name.Length is < 1 or > 150)
        {
            return ServiceResult<(string, string, int)>.Fail(ServiceErrorCodes.Validation, "Vui lòng nhập tên vật tư.");
        }

        var category = (request.Loai ?? string.Empty).Trim();
        if (category.Length is < 1 or > 50)
        {
            return ServiceResult<(string, string, int)>.Fail(ServiceErrorCodes.Validation, "Vui lòng nhập loại vật tư.");
        }

        if (request.SoLuongTon < 0)
        {
            return ServiceResult<(string, string, int)>.Fail(ServiceErrorCodes.Validation, "Số lượng phải từ 0 trở lên.");
        }

        if (request.DonGia < 0)
        {
            return ServiceResult<(string, string, int)>.Fail(ServiceErrorCodes.Validation, "Đơn giá phải từ 0 trở lên.");
        }

        var manager = await _care.GetUserAsync(request.ManagedBy, cancellationToken);
        var keepsCurrent = currentManagerId.HasValue && request.ManagedBy == currentManagerId.Value;
        var active = manager is not null
            && string.Equals(manager.Status, UserStatuses.Active, StringComparison.OrdinalIgnoreCase);
        if (manager is null || (!active && !keepsCurrent))
        {
            return ServiceResult<(string, string, int)>.Fail(ServiceErrorCodes.Validation, "Vui lòng chọn người quản lý đang hoạt động.");
        }

        return ServiceResult<(string, string, int)>.Ok((name, category, manager.UserId));
    }

    private string ScheduleText(StableAssignment assignment, IReadOnlyList<CareTask>? tasks)
    {
        if (tasks is { Count: > 0 })
        {
            return string.Join(
                "\n",
                tasks
                    .Where(t => t.ScheduledAt.HasValue)
                    .OrderBy(t => t.ScheduledAt)
                    .Select(t => $"{ClockTime(t.ScheduledAt!.Value)} {TaskLabel(t)}"));
        }

        return assignment.Notes ?? string.Empty;
    }

    private CareTaskDto MapTask(CareTask task) => new()
    {
        TaskID = task.CareTaskId,
        HorseID = task.HorseId,
        GroomID = task.UserId ?? 0,
        LoaiViec = TaskLabel(task),
        TrangThai = CareTaskStatuses.IsCompleted(task.Status, task.CompletedAt),
        Ngay = DateText(task.ScheduledAt ?? task.CreatedAt),
        Gio = task.ScheduledAt.HasValue ? ClockTime(task.ScheduledAt.Value) : string.Empty,
    };

    private IncidentReportDto MapIncident(IncidentReport report) => new()
    {
        ReportID = report.ReportId,
        HorseID = report.HorseId,
        GroomID = report.GroomId,
        LoaiSuCo = report.IncidentType,
        MoTa = report.Description ?? string.Empty,
        HinhAnh = report.ImageUrl ?? string.Empty,
        Ngay = report.ReportedOn.ToString("yyyy-MM-dd"),
    };

    private static SupplyDto MapSupply(Supply supply) => new()
    {
        SupplyID = supply.SupplyId,
        TenVatTu = supply.SupplyName,
        Loai = supply.Category ?? string.Empty,
        SoLuongTon = supply.QuantityInStock,
        ManagedBy = supply.UserId ?? 0,
        DonGia = supply.UnitPrice ?? 0,
        DonVi = supply.Unit ?? string.Empty,
    };

    private static string TaskLabel(CareTask task) =>
        !string.IsNullOrWhiteSpace(task.TaskName)
            ? task.TaskName.Trim()
            : string.IsNullOrWhiteSpace(task.TaskType) ? "Công việc" : task.TaskType.Trim();

    private DateOnly LocalToday() => DateOnly.FromDateTime(_clock.GetLocalNow().DateTime);

    private DateOnly LocalDate(DateTime value) => DateOnly.FromDateTime(WallClock(value));

    private string DateText(DateTime value) => DateOnly.FromDateTime(WallClock(value)).ToString("yyyy-MM-dd");

    private string ClockTime(DateTime value) => WallClock(value).ToString("HH:mm");

    private DateTime WallClock(DateTime value) =>
        value.Kind == DateTimeKind.Utc
            ? TimeZoneInfo.ConvertTime(new DateTimeOffset(value, TimeSpan.Zero), _clock.LocalTimeZone).DateTime
            : value;

    private static bool IsClosedStable(string? status) =>
        ClosedStableStatuses.Any(closed => string.Equals(closed, status?.Trim(), StringComparison.OrdinalIgnoreCase));

    private static string? CanonicalRole(User user) =>
        RoleMapper.TryMapToCanonicalRoleId(user.Role.RoleName, out var roleId) ? roleId : null;

    private static string? ValidateImage(string? value, out string? stored)
    {
        stored = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (stored is null)
        {
            return null;
        }

        if (stored.Length > IncidentTypes.ImageMaxLength)
        {
            return "Ảnh tối đa 5MB.";
        }

        if (stored.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || stored.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || stored.StartsWith('/'))
        {
            return stored.Length <= 500 ? null : "Đường dẫn ảnh không hợp lệ.";
        }

        var acceptable = stored.StartsWith("data:image/jpeg", StringComparison.OrdinalIgnoreCase)
            || stored.StartsWith("data:image/jpg", StringComparison.OrdinalIgnoreCase)
            || stored.StartsWith("data:image/png", StringComparison.OrdinalIgnoreCase);
        return acceptable ? null : "Chỉ nhận ảnh JPG hoặc PNG.";
    }

    private static string NormalizeMeal(string? mealName, TimeOnly? feedingTime)
    {
        var folded = Fold(mealName ?? string.Empty);
        if (folded.Contains("sang") || folded.Contains("morning"))
        {
            return "Sáng";
        }

        if (folded.Contains("chieu") || folded.Contains("afternoon"))
        {
            return "Chiều";
        }

        if (folded.Contains("toi") || folded.Contains("evening") || folded.Contains("night"))
        {
            return "Tối";
        }

        if (feedingTime is { } time)
        {
            if (time < new TimeOnly(11, 0))
            {
                return "Sáng";
            }

            if (time < new TimeOnly(17, 0))
            {
                return "Chiều";
            }

            return "Tối";
        }

        var trimmed = (mealName ?? string.Empty).Trim();
        return trimmed.Length == 0 ? "Sáng" : trimmed;
    }

    private static int MealRank(string meal)
    {
        var index = Array.IndexOf(MealOrder, meal);
        return index < 0 ? MealOrder.Length : index;
    }

    private static void AddComponent(string? feedName, decimal quantity, ref decimal grain, ref decimal hay, ref decimal vitamin)
    {
        var folded = Fold(feedName ?? string.Empty);
        if (folded.Contains("vitamin"))
        {
            vitamin += quantity;
        }
        else if (folded is "co" || folded.StartsWith("co ") || folded.Contains(" co") || folded.Contains("hay"))
        {
            hay += quantity;
        }
        else
        {
            grain += quantity;
        }
    }

    private static string Fold(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string FormatQty(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
