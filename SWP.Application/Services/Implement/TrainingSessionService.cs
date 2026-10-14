using SWP.Application.Common;
using SWP.Application.DTOs.Training;
using SWP.Application.Services.Interface;
using SWP.Application.Training;
using SWP.Domain.Common;
using SWP.Domain.Models.Entity;
using SWP.Infrastructure.Repositories.Interface;

namespace SWP.Application.Services.Implement;

public class TrainingSessionService : ITrainingSessionService
{
    private const string SessionNotFound = "Không tìm thấy buổi tập.";

    private readonly ITrainingSessionRepository _sessionRepository;
    private readonly ITrainingPlanRepository _planRepository;
    private readonly IHorseRepository _horseRepository;
    private readonly IUserRepository _userRepository;
    private readonly IHorseFitnessService _fitnessService;
    private readonly TimeProvider _timeProvider;

    public TrainingSessionService(
        ITrainingSessionRepository sessionRepository,
        ITrainingPlanRepository planRepository,
        IHorseRepository horseRepository,
        IUserRepository userRepository,
        IHorseFitnessService fitnessService,
        TimeProvider timeProvider)
    {
        _sessionRepository = sessionRepository;
        _planRepository = planRepository;
        _horseRepository = horseRepository;
        _userRepository = userRepository;
        _fitnessService = fitnessService;
        _timeProvider = timeProvider;
    }

    private DateTime LocalNow => _timeProvider.GetLocalNow().DateTime;

    private DateOnly Today => DateOnly.FromDateTime(LocalNow);

    private sealed record ValidatedSession(
        TrainingPlanPhase Phase,
        string TrainingType,
        string Surface,
        TimeOnly StartTime,
        User? Assignee);

    public async Task<ServiceResult<IReadOnlyList<TrainingSessionDto>>> SearchAsync(
        TrainingSessionQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var rangeError = ResolveRange(query.From, query.To, out var from, out var to);
        if (rangeError is not null)
        {
            return ServiceResult<IReadOnlyList<TrainingSessionDto>>.Invalid(rangeError);
        }

        string? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!TrainingSessionStatuses.TryNormalize(query.Status, out var normalized))
            {
                return ServiceResult<IReadOnlyList<TrainingSessionDto>>.Invalid("Trạng thái buổi tập không hợp lệ.");
            }

            statusFilter = normalized;
        }

        var sessions = await _sessionRepository.SearchAsync(
            from, to, query.HorseId, query.PlanId, query.AssignedToUserId, statusFilter, cancellationToken);
        return ServiceResult<IReadOnlyList<TrainingSessionDto>>.Ok(
            sessions.Select(s => TrainingMapper.ToSessionDto(s)).ToList());
    }

    public async Task<ServiceResult<TrainingSessionDto>> GetByIdAsync(
        int sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        return session is null
            ? ServiceResult<TrainingSessionDto>.NotFound(SessionNotFound)
            : ServiceResult<TrainingSessionDto>.Ok(TrainingMapper.ToSessionDto(session));
    }

    public async Task<ServiceResult<TrainingSessionDto>> CreateAsync(
        int planId,
        SaveTrainingSessionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var plan = await _planRepository.GetForUpdateAsync(planId, cancellationToken);
        if (plan is null)
        {
            return ServiceResult<TrainingSessionDto>.NotFound("Không tìm thấy giáo án.");
        }

        var validation = await ValidateSessionAsync(plan, request, excludeSessionId: null, cancellationToken);
        if (!validation.Success)
        {
            return validation.AsFailure<TrainingSessionDto>();
        }

        var session = new TrainingSchedule
        {
            PlanId = plan.PlanId,
            Status = TrainingSessionStatuses.Scheduled,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
        };
        ApplySessionFields(session, request, validation.Data!);

        await _sessionRepository.AddAsync(session, cancellationToken);
        return ServiceResult<TrainingSessionDto>.Ok(TrainingMapper.ToSessionDto(session, plan));
    }

    public async Task<ServiceResult<TrainingSessionDto>> UpdateAsync(
        int sessionId,
        SaveTrainingSessionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetForUpdateAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return ServiceResult<TrainingSessionDto>.NotFound(SessionNotFound);
        }

        if (!TrainingSessionStatuses.Is(session.Status, TrainingSessionStatuses.Scheduled))
        {
            return ServiceResult<TrainingSessionDto>.Fail(
                ServiceErrorCodes.InvalidStatus,
                "Chỉ sửa được buổi tập chưa bắt đầu.");
        }

        var validation = await ValidateSessionAsync(session.Plan, request, sessionId, cancellationToken);
        if (!validation.Success)
        {
            return validation.AsFailure<TrainingSessionDto>();
        }

        ApplySessionFields(session, request, validation.Data!);
        await _sessionRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult<TrainingSessionDto>.Ok(TrainingMapper.ToSessionDto(session, session.Plan));
    }

    public async Task<ServiceResult<TrainingSessionDto>> AssignAsync(
        int sessionId,
        AssignTrainingSessionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetForUpdateAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return ServiceResult<TrainingSessionDto>.NotFound(SessionNotFound);
        }

        if (!IsOpen(session))
        {
            return ServiceResult<TrainingSessionDto>.Fail(
                ServiceErrorCodes.InvalidStatus,
                "Buổi tập đã hoàn thành hoặc đã hủy – không thể phân công.");
        }

        var (assigneeError, assignee) = await TrainingValidation.ValidateAssigneeAsync(
            _userRepository, request.AssignedToUserId, cancellationToken);
        if (assigneeError is not null)
        {
            return ServiceResult<TrainingSessionDto>.Invalid(assigneeError);
        }

        session.AssignedToUserId = assignee?.UserId;
        session.AssignedToUser = assignee;
        await _sessionRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult<TrainingSessionDto>.Ok(TrainingMapper.ToSessionDto(session, session.Plan));
    }

    public async Task<ServiceResult<TrainingSessionDto>> StartAsync(
        int sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetForUpdateAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return ServiceResult<TrainingSessionDto>.NotFound(SessionNotFound);
        }

        if (!TrainingSessionStatuses.Is(session.Status, TrainingSessionStatuses.Scheduled))
        {
            return ServiceResult<TrainingSessionDto>.Fail(
                ServiceErrorCodes.InvalidStatus,
                $"Chỉ bắt đầu được buổi tập đang ở trạng thái Scheduled (hiện tại: {session.Status}).");
        }

        if (!TrainingPlanStatuses.Is(session.Plan.PlanStatus, TrainingPlanStatuses.Active))
        {
            return ServiceResult<TrainingSessionDto>.Fail(
                ServiceErrorCodes.InvalidStatus,
                "Giáo án của buổi tập không còn được áp dụng.");
        }

        if (DateOnly.FromDateTime(session.TrainingDate) != Today)
        {
            return ServiceResult<TrainingSessionDto>.Invalid("Chỉ bắt đầu được buổi tập của ngày hôm nay.");
        }

        // Kiểm tra lại thể trạng ngay trước khi tập: bác sĩ có thể đã khóa huấn luyện sau khi xếp lịch.
        var intensity = TrainingTypes.GetIntensity(session.TrainingType) ?? TrainingIntensityLevels.Medium;
        var fitnessError = await CheckFitnessAsync(session.Plan.HorseId, intensity, session.TrainingType, cancellationToken);
        if (fitnessError is not null)
        {
            return fitnessError;
        }

        session.Status = TrainingSessionStatuses.InProgress;
        session.StartTime = LocalNow;
        await _sessionRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult<TrainingSessionDto>.Ok(TrainingMapper.ToSessionDto(session, session.Plan));
    }

    public async Task<ServiceResult<TrainingSessionDto>> CompleteAsync(
        int sessionId,
        CompleteTrainingSessionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetForUpdateAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return ServiceResult<TrainingSessionDto>.NotFound(SessionNotFound);
        }

        if (!TrainingSessionStatuses.Is(session.Status, TrainingSessionStatuses.InProgress))
        {
            return ServiceResult<TrainingSessionDto>.Fail(
                ServiceErrorCodes.InvalidStatus,
                "Chỉ ghi nhận kết quả cho buổi tập đang diễn ra – hãy bắt đầu buổi tập trước.");
        }

        var error = ValidateCompletion(request);
        if (error is not null)
        {
            return ServiceResult<TrainingSessionDto>.Invalid(error);
        }

        session.ActualDistanceKm = request.ActualDistanceKm;
        session.ActualDurationMinutes = request.ActualDurationMinutes;
        session.PerformanceScore = request.PerformanceScore;
        session.AvgHeartRate = request.AvgHeartRate;
        session.MaxHeartRate = request.MaxHeartRate;
        session.AvgSpeedKmh = request.AvgSpeedKmh;
        session.Result = TrainingValidation.NormalizeOptional(request.Result);
        session.TrainerComment = request.TrainerComment.Trim();
        session.TrialVideoUrl = TrainingValidation.NormalizeOptional(request.TrialVideoUrl);
        session.EndTime = LocalNow;
        session.Status = TrainingSessionStatuses.Completed;

        await _sessionRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult<TrainingSessionDto>.Ok(TrainingMapper.ToSessionDto(session, session.Plan));
    }

    public async Task<ServiceResult<TrainingSessionDto>> CancelAsync(
        int sessionId,
        CancelTrainingSessionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetForUpdateAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return ServiceResult<TrainingSessionDto>.NotFound(SessionNotFound);
        }

        if (!IsOpen(session))
        {
            return ServiceResult<TrainingSessionDto>.Fail(
                ServiceErrorCodes.InvalidStatus,
                "Buổi tập đã hoàn thành hoặc đã hủy.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return ServiceResult<TrainingSessionDto>.Invalid("Vui lòng nhập lý do hủy.");
        }

        if (request.Reason.Trim().Length > 450)
        {
            return ServiceResult<TrainingSessionDto>.Invalid("Lý do hủy tối đa 450 ký tự.");
        }

        if (TrainingSessionStatuses.Is(session.Status, TrainingSessionStatuses.InProgress))
        {
            session.EndTime = LocalNow;
        }

        session.Status = TrainingSessionStatuses.Cancelled;
        session.Result = $"Đã hủy: {request.Reason.Trim()}";
        await _sessionRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult<TrainingSessionDto>.Ok(TrainingMapper.ToSessionDto(session, session.Plan));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(int sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetForUpdateAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return ServiceResult<bool>.NotFound(SessionNotFound);
        }

        if (!TrainingSessionStatuses.Is(session.Status, TrainingSessionStatuses.Scheduled))
        {
            return ServiceResult<bool>.Fail(
                ServiceErrorCodes.InvalidStatus,
                "Chỉ xóa được buổi tập chưa bắt đầu; buổi khác hãy dùng chức năng hủy.");
        }

        await _sessionRepository.DeleteAsync(session, cancellationToken);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<IReadOnlyList<TrainingStaffDto>> GetAssignableStaffAsync(CancellationToken cancellationToken = default)
    {
        var grooms = await _userRepository.GetActiveUsersByCanonicalRoleAsync(RoleNames.Groom, cancellationToken);
        var trainers = await _userRepository.GetActiveUsersByCanonicalRoleAsync(RoleNames.HeadTrainer, cancellationToken);

        return grooms.Select(u => (User: u, Role: RoleNames.Groom))
            .Concat(trainers.Select(u => (User: u, Role: RoleNames.HeadTrainer)))
            .Select(x => new TrainingStaffDto
            {
                UserId = x.User.UserId,
                FullName = x.User.FullName,
                Email = x.User.Email,
                RoleId = x.Role,
            })
            .ToList();
    }

    public async Task<ServiceResult<IReadOnlyList<TrainingSessionDto>>> GetMyAssignmentsAsync(
        int userId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        var rangeError = ResolveRange(from, to, out var rangeFrom, out var rangeTo);
        if (rangeError is not null)
        {
            return ServiceResult<IReadOnlyList<TrainingSessionDto>>.Invalid(rangeError);
        }

        var sessions = await _sessionRepository.SearchAsync(
            rangeFrom, rangeTo, assignedToUserId: userId, cancellationToken: cancellationToken);
        return ServiceResult<IReadOnlyList<TrainingSessionDto>>.Ok(sessions
            .Where(s => !TrainingSessionStatuses.Is(s.Status, TrainingSessionStatuses.Cancelled))
            .Select(s => TrainingMapper.ToSessionDto(s))
            .ToList());
    }

    public async Task<ServiceResult<HorseWeeklyScheduleDto>> GetHorseWeeklyScheduleAsync(
        int horseId,
        DateOnly? weekStart,
        CancellationToken cancellationToken = default)
    {
        var horse = await _horseRepository.GetByIdAsync(horseId, cancellationToken);
        if (horse is null)
        {
            return ServiceResult<HorseWeeklyScheduleDto>.NotFound("Không tìm thấy ngựa.");
        }

        var start = TrainingRules.WeekStartOf(weekStart ?? Today);
        var end = start.AddDays(6);
        var sessions = (await _sessionRepository.SearchAsync(start, end, horseId, cancellationToken: cancellationToken))
            .Select(s => TrainingMapper.ToSessionDto(s))
            .ToList();

        var active = sessions.Where(s => !TrainingSessionStatuses.Is(s.Status, TrainingSessionStatuses.Cancelled)).ToList();
        var completed = active.Where(s => TrainingSessionStatuses.Is(s.Status, TrainingSessionStatuses.Completed)).ToList();
        var scores = completed.Where(s => s.PerformanceScore.HasValue).Select(s => s.PerformanceScore!.Value).ToList();

        return ServiceResult<HorseWeeklyScheduleDto>.Ok(new HorseWeeklyScheduleDto
        {
            HorseId = horse.HorseId,
            HorseName = horse.HorseName,
            WeekStartDate = start,
            WeekEndDate = end,
            Days = Enumerable.Range(0, 7)
                .Select(offset =>
                {
                    var date = start.AddDays(offset);
                    var daySessions = sessions.Where(s => s.TrainingDate == date).ToList();
                    return new WeeklyScheduleDayDto
                    {
                        Date = date,
                        DayOfWeek = TrainingRules.DayOfWeekLabel(date),
                        IsRestDay = !daySessions.Any(s =>
                            !TrainingSessionStatuses.Is(s.Status, TrainingSessionStatuses.Cancelled)),
                        Sessions = daySessions,
                    };
                })
                .ToList(),
            Summary = new WeeklyScheduleSummaryDto
            {
                TotalSessions = active.Count,
                CompletedSessions = completed.Count,
                CancelledSessions = sessions.Count - active.Count,
                PlannedDistanceKm = active.Sum(s => s.DistanceKm ?? 0),
                ActualDistanceKm = completed.Sum(s => s.ActualDistanceKm ?? 0),
                PlannedDurationMinutes = active.Sum(s => s.DurationMinutes ?? 0),
                AveragePerformanceScore = scores.Count == 0 ? null : Math.Round(scores.Average(), 2),
            },
        });
    }

    private async Task<ServiceResult<ValidatedSession>> ValidateSessionAsync(
        TrainingPlan plan,
        SaveTrainingSessionRequestDto request,
        int? excludeSessionId,
        CancellationToken cancellationToken)
    {
        if (!TrainingPlanStatuses.Is(plan.PlanStatus, TrainingPlanStatuses.Active))
        {
            return ServiceResult<ValidatedSession>.Fail(
                ServiceErrorCodes.InvalidStatus,
                "Chỉ xếp lịch cho giáo án đang áp dụng.");
        }

        var date = request.TrainingDate;
        if (date == default)
        {
            return ServiceResult<ValidatedSession>.Invalid("Vui lòng chọn ngày tập.");
        }

        if (date < Today)
        {
            return ServiceResult<ValidatedSession>.Invalid("Không thể xếp lịch cho ngày đã qua.");
        }

        if (date < plan.StartDate || (plan.EndDate is { } planEnd && date > planEnd))
        {
            return ServiceResult<ValidatedSession>.Invalid("Ngày tập nằm ngoài thời gian của giáo án.");
        }

        var phase = plan.Phases.FirstOrDefault(p => p.StartDate <= date && date <= p.EndDate);
        if (phase is null)
        {
            return ServiceResult<ValidatedSession>.Invalid("Ngày tập không thuộc giai đoạn nào của giáo án.");
        }

        if (!TrainingTypes.TryNormalize(request.TrainingType, out var trainingType))
        {
            return ServiceResult<ValidatedSession>.Invalid("Loại bài tập không hợp lệ.");
        }

        var intensity = TrainingTypes.GetIntensity(trainingType)!;
        if (!TrainingIntensityLevels.IsWithin(intensity, phase.IntensityLevel))
        {
            return ServiceResult<ValidatedSession>.Invalid(
                $"Bài {TrainingTypes.Labels[trainingType]} (cường độ {intensity}) vượt cường độ tối đa của giai đoạn '{phase.PhaseName}' ({phase.IntensityLevel}).");
        }

        string surface;
        if (string.IsNullOrWhiteSpace(request.Surface))
        {
            surface = WeeklyScheduleGenerator.ResolveSurface(trainingType, phase.Surface);
        }
        else if (!TrackSurfaces.TryNormalize(request.Surface, out surface))
        {
            return ServiceResult<ValidatedSession>.Invalid("Mặt sân không hợp lệ.");
        }

        if (!TrackSurfaces.IsCompatible(trainingType, surface))
        {
            return ServiceResult<ValidatedSession>.Invalid("Bài bơi phải tập ở hồ bơi và hồ bơi chỉ dùng cho bài bơi.");
        }

        var volumeError = TrainingValidation.ValidateDistance(request.DistanceKm)
                          ?? TrainingValidation.ValidateDuration(request.DurationMinutes);
        if (volumeError is not null)
        {
            return ServiceResult<ValidatedSession>.Invalid($"Buổi tập: {volumeError}");
        }

        if (request.SessionTitle?.Trim().Length > 150)
        {
            return ServiceResult<ValidatedSession>.Invalid("Tiêu đề buổi tập tối đa 150 ký tự.");
        }

        if (request.Notes?.Trim().Length > 1000)
        {
            return ServiceResult<ValidatedSession>.Invalid("Ghi chú tối đa 1000 ký tự.");
        }

        if (!TrainingValidation.TryParseTime(request.StartTime, TrainingRules.DefaultStartTime, out var startTime))
        {
            return ServiceResult<ValidatedSession>.Invalid("Giờ bắt đầu phải có dạng HH:mm.");
        }

        var fitnessError = await CheckFitnessAsync(plan.HorseId, intensity, trainingType, cancellationToken);
        if (fitnessError is not null)
        {
            return fitnessError.AsFailure<ValidatedSession>();
        }

        var sameDay = await _sessionRepository.CountActiveSessionsOnDateAsync(
            plan.HorseId, date, excludeSessionId, cancellationToken);
        if (sameDay >= TrainingRules.MaxSessionsPerDay)
        {
            return ServiceResult<ValidatedSession>.Fail(
                ServiceErrorCodes.Conflict,
                $"Ngựa đã có {TrainingRules.MaxSessionsPerDay} buổi tập trong ngày {date:dd/MM/yyyy}.");
        }

        var (assigneeError, assignee) = await TrainingValidation.ValidateAssigneeAsync(
            _userRepository, request.AssignedToUserId, cancellationToken);
        if (assigneeError is not null)
        {
            return ServiceResult<ValidatedSession>.Invalid(assigneeError);
        }

        return ServiceResult<ValidatedSession>.Ok(new ValidatedSession(phase, trainingType, surface, startTime, assignee));
    }

    private async Task<ServiceResult<TrainingSessionDto>?> CheckFitnessAsync(
        int horseId,
        string intensity,
        string? trainingType,
        CancellationToken cancellationToken)
    {
        var fitness = await _fitnessService.GetAssessmentAsync(horseId, cancellationToken);
        if (fitness is null)
        {
            return ServiceResult<TrainingSessionDto>.NotFound("Không tìm thấy ngựa.");
        }

        if (!fitness.CanTrain || !TrainingIntensityLevels.IsWithin(intensity, fitness.MaxAllowedIntensity))
        {
            var label = TrainingTypes.TryNormalize(trainingType, out var type) ? TrainingTypes.Labels[type] : trainingType;
            var limit = fitness.MaxAllowedIntensity ?? "không được tập";
            return ServiceResult<TrainingSessionDto>.Fail(
                ServiceErrorCodes.HorseNotFit,
                $"Thể trạng hiện tại của ngựa không cho phép bài {label} (cường độ {intensity}; mức tối đa: {limit}).",
                fitness.Warnings);
        }

        return null;
    }

    private static void ApplySessionFields(
        TrainingSchedule session,
        SaveTrainingSessionRequestDto request,
        ValidatedSession validated)
    {
        session.PhaseId = validated.Phase.PhaseId;
        session.Phase = validated.Phase;
        session.SessionTitle = TrainingValidation.NormalizeOptional(request.SessionTitle)
                               ?? $"{validated.Phase.PhaseName} - {TrainingTypes.Labels[validated.TrainingType]}";
        session.TrainingDate = request.TrainingDate.ToDateTime(TimeOnly.MinValue);
        session.StartTime = request.TrainingDate.ToDateTime(validated.StartTime);
        session.TrainingType = validated.TrainingType;
        session.DistanceKm = request.DistanceKm;
        session.DurationMinutes = request.DurationMinutes;
        session.Surface = validated.Surface;
        session.Notes = TrainingValidation.NormalizeOptional(request.Notes);
        session.AssignedToUserId = validated.Assignee?.UserId;
        session.AssignedToUser = validated.Assignee;
    }

    private static string? ValidateCompletion(CompleteTrainingSessionRequestDto request)
    {
        if (request.ActualDistanceKm <= 0 || request.ActualDistanceKm > TrainingRules.MaxActualDistanceKm)
        {
            return $"Cự ly thực tế phải lớn hơn 0 và không quá {TrainingRules.MaxActualDistanceKm} km.";
        }

        if (request.ActualDurationMinutes <= 0 || request.ActualDurationMinutes > TrainingRules.MaxActualDurationMinutes)
        {
            return $"Thời lượng thực tế phải từ 1 đến {TrainingRules.MaxActualDurationMinutes} phút.";
        }

        if (request.PerformanceScore < 0 || request.PerformanceScore > TrainingRules.MaxPerformanceScore)
        {
            return $"Điểm phong độ phải từ 0 đến {TrainingRules.MaxPerformanceScore}.";
        }

        if (!IsValidHeartRate(request.AvgHeartRate) || !IsValidHeartRate(request.MaxHeartRate))
        {
            return $"Nhịp tim phải từ {TrainingRules.MinHeartRate} đến {TrainingRules.MaxHeartRate} bpm.";
        }

        if (request.AvgHeartRate is { } avg && request.MaxHeartRate is { } max && max < avg)
        {
            return "Nhịp tim tối đa không được nhỏ hơn nhịp tim trung bình.";
        }

        if (request.AvgSpeedKmh is { } speed && (speed <= 0 || speed > TrainingRules.MaxSpeedKmh))
        {
            return $"Vận tốc trung bình phải lớn hơn 0 và không quá {TrainingRules.MaxSpeedKmh} km/h.";
        }

        if (request.Result?.Trim().Length > 500)
        {
            return "Kết quả tối đa 500 ký tự.";
        }

        if (string.IsNullOrWhiteSpace(request.TrainerComment))
        {
            return "Vui lòng nhập nhận xét chuyên môn sau buổi tập.";
        }

        if (request.TrainerComment.Trim().Length > 1000)
        {
            return "Nhận xét tối đa 1000 ký tự.";
        }

        if (!string.IsNullOrWhiteSpace(request.TrialVideoUrl))
        {
            var url = request.TrialVideoUrl.Trim();
            if (url.Length > 500
                || !Uri.TryCreate(url, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return "Link video phải là URL http/https hợp lệ (tối đa 500 ký tự).";
            }
        }

        return null;
    }

    private static bool IsValidHeartRate(decimal? value) =>
        value is null || (value >= TrainingRules.MinHeartRate && value <= TrainingRules.MaxHeartRate);

    private static bool IsOpen(TrainingSchedule session) =>
        TrainingSessionStatuses.Is(session.Status, TrainingSessionStatuses.Scheduled)
        || TrainingSessionStatuses.Is(session.Status, TrainingSessionStatuses.InProgress);

    private string? ResolveRange(DateOnly? from, DateOnly? to, out DateOnly rangeFrom, out DateOnly rangeTo)
    {
        rangeFrom = from ?? Today;
        rangeTo = to ?? rangeFrom.AddDays(6);
        if (rangeTo < rangeFrom)
        {
            return "Ngày kết thúc phải sau hoặc bằng ngày bắt đầu.";
        }

        if (rangeTo.DayNumber - rangeFrom.DayNumber >= TrainingRules.MaxQueryRangeDays)
        {
            return $"Chỉ xem tối đa {TrainingRules.MaxQueryRangeDays} ngày mỗi lần.";
        }

        return null;
    }
}
