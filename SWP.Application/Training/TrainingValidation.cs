using System.Globalization;
using SWP.Application.DTOs.Training;
using SWP.Domain.Common;
using SWP.Domain.Models.Entity;
using SWP.Infrastructure.Repositories.Interface;

namespace SWP.Application.Training;

/// <summary> Kiểm tra dữ liệu đầu vào dùng chung cho giáo án và buổi tập. </summary>
public static class TrainingValidation
{
    private static readonly string[] TimeFormats = ["HH:mm", "H:mm"];

    /// <summary> Phân tích giờ "HH:mm"; rỗng → <paramref name="fallback"/>. </summary>
    public static bool TryParseTime(string? value, TimeOnly fallback, out TimeOnly time)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            time = fallback;
            return true;
        }

        return TimeOnly.TryParseExact(
            value.Trim(),
            TimeFormats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out time);
    }

    public static string? ValidatePlanHeader(
        string? planName,
        string? objective,
        string? notes,
        string? trainingType,
        out string? normalizedTrainingType)
    {
        normalizedTrainingType = null;
        if (string.IsNullOrWhiteSpace(planName))
        {
            return "Vui lòng nhập tên giáo án.";
        }

        if (planName.Trim().Length > 150)
        {
            return "Tên giáo án tối đa 150 ký tự.";
        }

        if (objective?.Trim().Length > 500)
        {
            return "Mục tiêu tối đa 500 ký tự.";
        }

        if (notes?.Trim().Length > 1000)
        {
            return "Ghi chú tối đa 1000 ký tự.";
        }

        if (!string.IsNullOrWhiteSpace(trainingType))
        {
            if (!TrainingPlanTypes.TryNormalize(trainingType, out var planType))
            {
                return "Loại giáo án không hợp lệ.";
            }

            normalizedTrainingType = planType;
        }

        return null;
    }

    /// <summary> Kiểm tra và chuẩn hóa danh sách giai đoạn; kết quả đã sắp theo ngày và đánh số thứ tự. </summary>
    public static string? ValidatePhases(
        IReadOnlyList<TrainingPhaseRequestDto>? phases,
        DateOnly today,
        DateTime createdAtUtc,
        out List<TrainingPlanPhase> normalized)
    {
        normalized = new List<TrainingPlanPhase>();
        if (phases is null || phases.Count == 0)
        {
            return "Giáo án phải có ít nhất một giai đoạn.";
        }

        if (phases.Count > TrainingRules.MaxPhasesPerPlan)
        {
            return $"Giáo án tối đa {TrainingRules.MaxPhasesPerPlan} giai đoạn.";
        }

        for (var i = 0; i < phases.Count; i++)
        {
            var phase = phases[i];
            var label = $"Giai đoạn {i + 1}";

            if (string.IsNullOrWhiteSpace(phase.PhaseName))
            {
                return $"{label}: vui lòng nhập tên giai đoạn.";
            }

            if (phase.PhaseName.Trim().Length > 100)
            {
                return $"{label}: tên giai đoạn tối đa 100 ký tự.";
            }

            if (phase.Goal?.Trim().Length > 500)
            {
                return $"{label}: mục tiêu tối đa 500 ký tự.";
            }

            if (phase.StartDate > phase.EndDate)
            {
                return $"{label}: ngày bắt đầu phải trước hoặc bằng ngày kết thúc.";
            }

            if (phase.StartDate < today)
            {
                return $"{label}: không được bắt đầu trong quá khứ.";
            }

            if (!TrainingIntensityLevels.TryNormalize(phase.IntensityLevel, out var intensity))
            {
                return $"{label}: cường độ không hợp lệ (Recovery, Low, Medium, High).";
            }

            if (phase.SessionsPerWeek < TrainingRules.MinSessionsPerWeek
                || phase.SessionsPerWeek > TrainingRules.MaxSessionsPerWeek)
            {
                return $"{label}: số buổi/tuần phải từ {TrainingRules.MinSessionsPerWeek} đến {TrainingRules.MaxSessionsPerWeek} (cần ít nhất 1 ngày nghỉ).";
            }

            var distanceError = ValidateDistance(phase.TargetDistanceKm);
            if (distanceError is not null)
            {
                return $"{label}: {distanceError}";
            }

            var durationError = ValidateDuration(phase.SessionDurationMinutes);
            if (durationError is not null)
            {
                return $"{label}: {durationError}";
            }

            string? surface = null;
            if (!string.IsNullOrWhiteSpace(phase.Surface))
            {
                if (!TrackSurfaces.TryNormalize(phase.Surface, out var normalizedSurface))
                {
                    return $"{label}: mặt sân không hợp lệ.";
                }

                surface = normalizedSurface;
            }

            normalized.Add(new TrainingPlanPhase
            {
                PhaseName = phase.PhaseName.Trim(),
                Goal = NormalizeOptional(phase.Goal),
                StartDate = phase.StartDate,
                EndDate = phase.EndDate,
                IntensityLevel = intensity,
                SessionsPerWeek = phase.SessionsPerWeek,
                TargetDistanceKm = phase.TargetDistanceKm,
                SessionDurationMinutes = phase.SessionDurationMinutes,
                Surface = surface,
                CreatedAt = createdAtUtc,
            });
        }

        normalized = normalized.OrderBy(p => p.StartDate).ToList();
        for (var i = 1; i < normalized.Count; i++)
        {
            if (normalized[i].StartDate <= normalized[i - 1].EndDate)
            {
                return $"Giai đoạn '{normalized[i - 1].PhaseName}' và '{normalized[i].PhaseName}' bị trùng ngày.";
            }
        }

        var span = normalized[^1].EndDate.DayNumber - normalized[0].StartDate.DayNumber + 1;
        if (span > TrainingRules.MaxPlanDays)
        {
            return $"Giáo án kéo dài tối đa {TrainingRules.MaxPlanDays} ngày.";
        }

        for (var i = 0; i < normalized.Count; i++)
        {
            normalized[i].PhaseOrder = i + 1;
        }

        return null;
    }

    public static string? ValidateDistance(decimal distanceKm) =>
        distanceKm <= 0 || distanceKm > TrainingRules.MaxDistanceKm
            ? $"cự ly phải lớn hơn 0 và không quá {TrainingRules.MaxDistanceKm} km."
            : null;

    public static string? ValidateDuration(int durationMinutes) =>
        durationMinutes < TrainingRules.MinDurationMinutes || durationMinutes > TrainingRules.MaxDurationMinutes
            ? $"thời lượng phải từ {TrainingRules.MinDurationMinutes} đến {TrainingRules.MaxDurationMinutes} phút."
            : null;

    /// <summary> Người được phân công phải là Nhân viên chăm sóc hoặc HLV đang hoạt động. </summary>
    public static async Task<(string? Error, User? User)> ValidateAssigneeAsync(
        IUserRepository userRepository,
        int? userId,
        CancellationToken cancellationToken)
    {
        if (userId is null)
        {
            return (null, null);
        }

        var user = await userRepository.GetByIdWithRoleAsync(userId.Value, cancellationToken);
        if (user is null)
        {
            return ("Không tìm thấy nhân sự được phân công.", null);
        }

        if (!string.Equals(user.Status, UserStatuses.Active, StringComparison.OrdinalIgnoreCase))
        {
            return ("Nhân sự được phân công không ở trạng thái hoạt động.", null);
        }

        var role = RoleMapper.ToRoleId(user.Role.RoleName);
        if (role != RoleNames.Groom && role != RoleNames.HeadTrainer)
        {
            return ("Chỉ phân công buổi tập cho Nhân viên chăm sóc hoặc HLV.", null);
        }

        return (null, user);
    }

    public static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
