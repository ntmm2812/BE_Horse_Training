using SWP.Application.Common;
using SWP.Application.DTOs.Training;
using SWP.Application.Services.Interface;
using SWP.Application.Training;
using SWP.Domain.Common;
using SWP.Domain.Models.Entity;
using SWP.Infrastructure.Repositories.Interface;

namespace SWP.Application.Services.Implement;

public class TrainingPlanService : ITrainingPlanService
{
    private const string PlanNotFound = "Không tìm thấy giáo án.";
    private const string HorseNotFound = "Không tìm thấy ngựa.";

    private readonly ITrainingPlanRepository _planRepository;
    private readonly ITrainingSessionRepository _sessionRepository;
    private readonly IHorseRepository _horseRepository;
    private readonly IUserRepository _userRepository;
    private readonly IHorseFitnessService _fitnessService;
    private readonly TimeProvider _timeProvider;

    public TrainingPlanService(
        ITrainingPlanRepository planRepository,
        ITrainingSessionRepository sessionRepository,
        IHorseRepository horseRepository,
        IUserRepository userRepository,
        IHorseFitnessService fitnessService,
        TimeProvider timeProvider)
    {
        _planRepository = planRepository;
        _sessionRepository = sessionRepository;
        _horseRepository = horseRepository;
        _userRepository = userRepository;
        _fitnessService = fitnessService;
        _timeProvider = timeProvider;
    }

    private DateOnly Today => DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);

    private DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

    public TrainingOptionsDto GetOptions() => TrainingMapper.BuildOptions();

    public async Task<ServiceResult<IReadOnlyList<TrainingPlanDto>>> SearchAsync(
        int? horseId,
        string? status,
        CancellationToken cancellationToken = default)
    {
        string? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!TrainingPlanStatuses.TryNormalize(status, out var normalized))
            {
                return ServiceResult<IReadOnlyList<TrainingPlanDto>>.Invalid("Trạng thái giáo án không hợp lệ.");
            }

            statusFilter = normalized;
        }

        var plans = await _planRepository.SearchAsync(horseId, statusFilter, cancellationToken);
        return ServiceResult<IReadOnlyList<TrainingPlanDto>>.Ok(plans.Select(TrainingMapper.ToPlanDto).ToList());
    }

    public async Task<ServiceResult<TrainingPlanDetailDto>> GetDetailAsync(
        int planId,
        CancellationToken cancellationToken = default)
    {
        var plan = await _planRepository.GetDetailAsync(planId, cancellationToken);
        return plan is null
            ? ServiceResult<TrainingPlanDetailDto>.NotFound(PlanNotFound)
            : ServiceResult<TrainingPlanDetailDto>.Ok(TrainingMapper.ToPlanDetailDto(plan));
    }

    public async Task<ServiceResult<TrainingPlanSuggestionDto>> GetSuggestionAsync(
        int horseId,
        DateOnly? startDate,
        int? weeks,
        CancellationToken cancellationToken = default)
    {
        var totalWeeks = weeks ?? TrainingRules.DefaultSuggestionWeeks;
        if (totalWeeks < 1 || totalWeeks > TrainingRules.MaxSuggestionWeeks)
        {
            return ServiceResult<TrainingPlanSuggestionDto>.Invalid(
                $"Số tuần phải từ 1 đến {TrainingRules.MaxSuggestionWeeks}.");
        }

        var fitness = await _fitnessService.GetAssessmentAsync(horseId, cancellationToken);
        if (fitness is null)
        {
            return ServiceResult<TrainingPlanSuggestionDto>.NotFound(HorseNotFound);
        }

        var today = Today;
        var thisMonday = TrainingRules.WeekStartOf(today);
        var start = startDate ?? (thisMonday == today ? today : thisMonday.AddDays(7));
        if (start < today)
        {
            return ServiceResult<TrainingPlanSuggestionDto>.Invalid("Ngày bắt đầu không được ở quá khứ.");
        }

        var suggestion = TrainingPlanSuggester.Suggest(fitness.RecommendedIntensity, fitness.AgeYears, start, totalWeeks);
        var phases = suggestion.Phases
            .Select(p => new TrainingPhaseRequestDto
            {
                PhaseName = p.PhaseName,
                Goal = p.Goal,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                IntensityLevel = p.IntensityLevel,
                SessionsPerWeek = p.SessionsPerWeek,
                TargetDistanceKm = p.TargetDistanceKm,
                SessionDurationMinutes = p.SessionDurationMinutes,
                Surface = p.Surface,
            })
            .ToList();

        return ServiceResult<TrainingPlanSuggestionDto>.Ok(new TrainingPlanSuggestionDto
        {
            HorseId = fitness.HorseId,
            HorseName = fitness.HorseName,
            CanCreatePlan = suggestion.CanCreatePlan,
            SuggestedPlanName = suggestion.CanCreatePlan ? $"Giáo án {fitness.HorseName} - {start:dd/MM/yyyy}" : null,
            SuggestedTrainingType = suggestion.PlanType,
            StartDate = start,
            EndDate = phases.Count == 0 ? null : phases[^1].EndDate,
            Phases = phases,
            Rationale = suggestion.Rationale,
            Fitness = fitness,
        });
    }

    public async Task<ServiceResult<TrainingPlanDetailDto>> CreateAsync(
        CreateTrainingPlanRequestDto request,
        int createdByUserId,
        CancellationToken cancellationToken = default)
    {
        if (request.HorseId <= 0)
        {
            return ServiceResult<TrainingPlanDetailDto>.Invalid("Vui lòng chọn ngựa.");
        }

        var headerError = TrainingValidation.ValidatePlanHeader(
            request.PlanName, request.Objective, request.Notes, request.TrainingType, out var planType);
        if (headerError is not null)
        {
            return ServiceResult<TrainingPlanDetailDto>.Invalid(headerError);
        }

        var horse = await _horseRepository.GetByIdAsync(request.HorseId, cancellationToken);
        if (horse is null)
        {
            return ServiceResult<TrainingPlanDetailDto>.NotFound(HorseNotFound);
        }

        var phaseError = TrainingValidation.ValidatePhases(request.Phases, Today, UtcNow, out var phases);
        if (phaseError is not null)
        {
            return ServiceResult<TrainingPlanDetailDto>.Invalid(phaseError);
        }

        var fitnessError = await CheckPhasesAgainstFitnessAsync(request.HorseId, phases, cancellationToken);
        if (fitnessError is not null)
        {
            return fitnessError;
        }

        var plan = new TrainingPlan
        {
            HorseId = request.HorseId,
            PlanName = request.PlanName.Trim(),
            Objective = TrainingValidation.NormalizeOptional(request.Objective),
            TrainingType = planType,
            Notes = TrainingValidation.NormalizeOptional(request.Notes),
            PlanStatus = TrainingPlanStatuses.Draft,
            CreatedByUserId = createdByUserId,
            CreatedAt = UtcNow,
        };
        ApplyPhases(plan, phases);

        await _planRepository.AddAsync(plan, cancellationToken);
        return await GetDetailAsync(plan.PlanId, cancellationToken);
    }

    public async Task<ServiceResult<TrainingPlanDetailDto>> UpdateAsync(
        int planId,
        UpdateTrainingPlanRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var plan = await _planRepository.GetForUpdateAsync(planId, cancellationToken);
        if (plan is null)
        {
            return ServiceResult<TrainingPlanDetailDto>.NotFound(PlanNotFound);
        }

        var isDraft = TrainingPlanStatuses.Is(plan.PlanStatus, TrainingPlanStatuses.Draft);
        if (!isDraft && !TrainingPlanStatuses.Is(plan.PlanStatus, TrainingPlanStatuses.Active))
        {
            return ServiceResult<TrainingPlanDetailDto>.Fail(
                ServiceErrorCodes.InvalidStatus,
                "Giáo án đã hoàn thành hoặc đã hủy – không thể chỉnh sửa.");
        }

        var headerError = TrainingValidation.ValidatePlanHeader(
            request.PlanName, request.Objective, request.Notes, request.TrainingType, out var planType);
        if (headerError is not null)
        {
            return ServiceResult<TrainingPlanDetailDto>.Invalid(headerError);
        }

        if (request.Phases is not null)
        {
            if (!isDraft)
            {
                return ServiceResult<TrainingPlanDetailDto>.Fail(
                    ServiceErrorCodes.InvalidStatus,
                    "Chỉ được thay đổi giai đoạn khi giáo án ở trạng thái Nháp.");
            }

            var phaseError = TrainingValidation.ValidatePhases(request.Phases, Today, UtcNow, out var phases);
            if (phaseError is not null)
            {
                return ServiceResult<TrainingPlanDetailDto>.Invalid(phaseError);
            }

            var fitnessError = await CheckPhasesAgainstFitnessAsync(plan.HorseId, phases, cancellationToken);
            if (fitnessError is not null)
            {
                return fitnessError;
            }

            // Giáo án Nháp chưa có buổi tập nên thay toàn bộ giai đoạn là an toàn.
            plan.Phases.Clear();
            ApplyPhases(plan, phases);
        }

        plan.PlanName = request.PlanName.Trim();
        plan.Objective = TrainingValidation.NormalizeOptional(request.Objective);
        plan.TrainingType = planType;
        plan.Notes = TrainingValidation.NormalizeOptional(request.Notes);
        plan.UpdatedAt = UtcNow;

        await _planRepository.SaveChangesAsync(cancellationToken);
        return await GetDetailAsync(planId, cancellationToken);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(int planId, CancellationToken cancellationToken = default)
    {
        var plan = await _planRepository.GetForUpdateAsync(planId, cancellationToken);
        if (plan is null)
        {
            return ServiceResult<bool>.NotFound(PlanNotFound);
        }

        if (!TrainingPlanStatuses.Is(plan.PlanStatus, TrainingPlanStatuses.Draft))
        {
            return ServiceResult<bool>.Fail(
                ServiceErrorCodes.InvalidStatus,
                "Chỉ xóa được giáo án Nháp. Giáo án đã áp dụng hãy dùng chức năng hủy.");
        }

        await _planRepository.DeleteAsync(plan, cancellationToken);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<TrainingPlanDetailDto>> ActivateAsync(
        int planId,
        CancellationToken cancellationToken = default)
    {
        var plan = await _planRepository.GetForUpdateAsync(planId, cancellationToken);
        if (plan is null)
        {
            return ServiceResult<TrainingPlanDetailDto>.NotFound(PlanNotFound);
        }

        if (!TrainingPlanStatuses.Is(plan.PlanStatus, TrainingPlanStatuses.Draft))
        {
            return ServiceResult<TrainingPlanDetailDto>.Fail(
                ServiceErrorCodes.InvalidStatus,
                $"Chỉ áp dụng được giáo án Nháp (trạng thái hiện tại: {plan.PlanStatus}).");
        }

        if (plan.Phases.Count == 0)
        {
            return ServiceResult<TrainingPlanDetailDto>.Invalid("Giáo án chưa có giai đoạn nào.");
        }

        if (plan.EndDate is { } endDate && endDate < Today)
        {
            return ServiceResult<TrainingPlanDetailDto>.Invalid(
                "Giáo án đã quá ngày kết thúc – hãy cập nhật lại các giai đoạn.");
        }

        if (await _planRepository.HasOtherActivePlanAsync(plan.HorseId, plan.PlanId, cancellationToken))
        {
            return ServiceResult<TrainingPlanDetailDto>.Fail(
                ServiceErrorCodes.Conflict,
                "Ngựa đang có một giáo án khác đang áp dụng. Hãy hoàn thành hoặc hủy giáo án đó trước.");
        }

        var fitness = await _fitnessService.GetAssessmentAsync(plan.HorseId, cancellationToken);
        if (fitness is null)
        {
            return ServiceResult<TrainingPlanDetailDto>.NotFound(HorseNotFound);
        }

        if (!fitness.CanTrain)
        {
            return ServiceResult<TrainingPlanDetailDto>.Fail(
                ServiceErrorCodes.HorseNotFit,
                "Ngựa hiện không đủ điều kiện tập luyện – chưa thể áp dụng giáo án.",
                fitness.Warnings);
        }

        plan.PlanStatus = TrainingPlanStatuses.Active;
        plan.UpdatedAt = UtcNow;
        await _planRepository.SaveChangesAsync(cancellationToken);
        return await GetDetailAsync(planId, cancellationToken);
    }

    public Task<ServiceResult<TrainingPlanDetailDto>> CompleteAsync(
        int planId,
        CancellationToken cancellationToken = default) =>
        CloseAsync(
            planId,
            TrainingPlanStatuses.Completed,
            [TrainingPlanStatuses.Active],
            "Hủy do giáo án đã kết thúc.",
            cancellationToken);

    public Task<ServiceResult<TrainingPlanDetailDto>> CancelAsync(
        int planId,
        CancellationToken cancellationToken = default) =>
        CloseAsync(
            planId,
            TrainingPlanStatuses.Cancelled,
            [TrainingPlanStatuses.Draft, TrainingPlanStatuses.Active],
            "Hủy do giáo án bị hủy.",
            cancellationToken);

    public async Task<ServiceResult<WeeklyScheduleGenerationResultDto>> GenerateWeeklyScheduleAsync(
        int planId,
        GenerateWeeklyScheduleRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var plan = await _planRepository.GetForUpdateAsync(planId, cancellationToken);
        if (plan is null)
        {
            return ServiceResult<WeeklyScheduleGenerationResultDto>.NotFound(PlanNotFound);
        }

        var isActive = TrainingPlanStatuses.Is(plan.PlanStatus, TrainingPlanStatuses.Active);
        var isDraft = TrainingPlanStatuses.Is(plan.PlanStatus, TrainingPlanStatuses.Draft);
        if (!isActive && !(request.PreviewOnly && isDraft))
        {
            return ServiceResult<WeeklyScheduleGenerationResultDto>.Fail(
                ServiceErrorCodes.InvalidStatus,
                "Chỉ xếp lịch tuần cho giáo án đang áp dụng. Giáo án Nháp chỉ được xem trước (previewOnly).");
        }

        if (request.WeekStartDate == default)
        {
            return ServiceResult<WeeklyScheduleGenerationResultDto>.Invalid("Vui lòng chọn tuần cần xếp lịch.");
        }

        if (!TrainingValidation.TryParseTime(request.DefaultStartTime, TrainingRules.DefaultStartTime, out var startTime))
        {
            return ServiceResult<WeeklyScheduleGenerationResultDto>.Invalid("Giờ bắt đầu phải có dạng HH:mm.");
        }

        var (assigneeError, assignee) = await TrainingValidation.ValidateAssigneeAsync(
            _userRepository, request.AssignedToUserId, cancellationToken);
        if (assigneeError is not null)
        {
            return ServiceResult<WeeklyScheduleGenerationResultDto>.Invalid(assigneeError);
        }

        var weekStart = TrainingRules.WeekStartOf(request.WeekStartDate);
        var weekEnd = weekStart.AddDays(6);
        if (weekEnd < plan.StartDate || (plan.EndDate is { } planEnd && weekStart > planEnd))
        {
            return ServiceResult<WeeklyScheduleGenerationResultDto>.Invalid(
                "Tuần được chọn nằm ngoài thời gian của giáo án.");
        }

        var fitness = await _fitnessService.GetAssessmentAsync(plan.HorseId, cancellationToken);
        if (fitness is null)
        {
            return ServiceResult<WeeklyScheduleGenerationResultDto>.NotFound(HorseNotFound);
        }

        if (!fitness.CanTrain)
        {
            return ServiceResult<WeeklyScheduleGenerationResultDto>.Fail(
                ServiceErrorCodes.HorseNotFit,
                "Ngựa hiện không đủ điều kiện tập luyện – không thể xếp lịch.",
                fitness.Warnings);
        }

        var today = Today;
        var existing = await _sessionRepository.SearchAsync(
            weekStart, weekEnd, horseId: plan.HorseId, cancellationToken: cancellationToken);
        var replaceable = request.ReplaceExisting
            ? existing
                .Where(s => s.PlanId == planId
                            && TrainingSessionStatuses.Is(s.Status, TrainingSessionStatuses.Scheduled)
                            && DateOnly.FromDateTime(s.TrainingDate) >= today)
                .Select(s => s.ScheduleId)
                .ToHashSet()
            : new HashSet<int>();
        var occupied = existing
            .Where(s => !TrainingSessionStatuses.Is(s.Status, TrainingSessionStatuses.Cancelled)
                        && !replaceable.Contains(s.ScheduleId))
            .Select(s => DateOnly.FromDateTime(s.TrainingDate))
            .ToHashSet();

        var output = WeeklyScheduleGenerator.Generate(new WeeklyGenerationInput
        {
            WeekStart = weekStart,
            Phases = plan.Phases
                .Select(p => new PhaseSpec(
                    p.PhaseId,
                    p.PhaseName,
                    p.StartDate,
                    p.EndDate,
                    p.IntensityLevel,
                    p.SessionsPerWeek,
                    p.TargetDistanceKm,
                    p.SessionDurationMinutes,
                    p.Surface))
                .ToList(),
            MaxAllowedIntensity = fitness.MaxAllowedIntensity,
            Today = today,
            OccupiedDates = occupied,
            StartTime = startTime,
        });

        var result = new WeeklyScheduleGenerationResultDto
        {
            PlanId = planId,
            WeekStartDate = weekStart,
            WeekEndDate = weekEnd,
            PreviewOnly = request.PreviewOnly,
            SkippedDays = output.SkippedDays
                .Select(d => new SkippedDayDto { Date = d.Date, Reason = d.Reason })
                .ToList(),
            Warnings = output.Warnings,
        };

        if (request.PreviewOnly)
        {
            result.Sessions = output.Sessions
                .Select(g => TrainingMapper.ToPreviewDto(g, plan, assignee?.UserId))
                .ToList();
            return ServiceResult<WeeklyScheduleGenerationResultDto>.Ok(result);
        }

        if (replaceable.Count > 0)
        {
            var toRemove = await _sessionRepository.GetScheduledForPlanInRangeAsync(
                planId, today > weekStart ? today : weekStart, weekEnd, cancellationToken);
            _sessionRepository.RemoveRange(toRemove);
            result.RemovedCount = toRemove.Count;
        }

        var createdAt = UtcNow;
        var entities = output.Sessions
            .Select(g => new TrainingSchedule
            {
                PlanId = plan.PlanId,
                PhaseId = g.PhaseId,
                SessionTitle = g.Title,
                TrainingDate = g.Date.ToDateTime(TimeOnly.MinValue),
                StartTime = g.Date.ToDateTime(g.StartTime),
                TrainingType = g.TrainingType,
                DistanceKm = g.DistanceKm,
                DurationMinutes = g.DurationMinutes,
                Surface = g.Surface,
                Status = TrainingSessionStatuses.Scheduled,
                AssignedToUserId = assignee?.UserId,
                AssignedToUser = assignee,
                CreatedAt = createdAt,
            })
            .ToList();

        await _sessionRepository.AddRangeAsync(entities, cancellationToken);

        result.CreatedCount = entities.Count;
        result.Sessions = entities.Select(e => TrainingMapper.ToSessionDto(e, plan)).ToList();
        return ServiceResult<WeeklyScheduleGenerationResultDto>.Ok(result);
    }

    private async Task<ServiceResult<TrainingPlanDetailDto>> CloseAsync(
        int planId,
        string targetStatus,
        string[] allowedFrom,
        string cancelReason,
        CancellationToken cancellationToken)
    {
        var plan = await _planRepository.GetForUpdateAsync(planId, cancellationToken);
        if (plan is null)
        {
            return ServiceResult<TrainingPlanDetailDto>.NotFound(PlanNotFound);
        }

        if (!allowedFrom.Any(s => TrainingPlanStatuses.Is(plan.PlanStatus, s)))
        {
            return ServiceResult<TrainingPlanDetailDto>.Fail(
                ServiceErrorCodes.InvalidStatus,
                $"Không thể chuyển giáo án từ trạng thái {plan.PlanStatus} sang {targetStatus}.");
        }

        var openSessions = await _sessionRepository.GetOpenSessionsForPlanAsync(planId, cancellationToken);
        if (openSessions.Any(s => TrainingSessionStatuses.Is(s.Status, TrainingSessionStatuses.InProgress)))
        {
            return ServiceResult<TrainingPlanDetailDto>.Fail(
                ServiceErrorCodes.InvalidStatus,
                "Còn buổi tập đang diễn ra – hãy hoàn thành hoặc hủy buổi đó trước.");
        }

        foreach (var session in openSessions)
        {
            session.Status = TrainingSessionStatuses.Cancelled;
            session.Result = cancelReason;
        }

        plan.PlanStatus = targetStatus;
        plan.UpdatedAt = UtcNow;
        await _planRepository.SaveChangesAsync(cancellationToken);
        return await GetDetailAsync(planId, cancellationToken);
    }

    /// <summary> Thể trạng hiện tại phải cho phép cường độ của mọi giai đoạn. </summary>
    private async Task<ServiceResult<TrainingPlanDetailDto>?> CheckPhasesAgainstFitnessAsync(
        int horseId,
        IReadOnlyList<TrainingPlanPhase> phases,
        CancellationToken cancellationToken)
    {
        var fitness = await _fitnessService.GetAssessmentAsync(horseId, cancellationToken);
        if (fitness is null)
        {
            return ServiceResult<TrainingPlanDetailDto>.NotFound(HorseNotFound);
        }

        if (!fitness.CanTrain)
        {
            return ServiceResult<TrainingPlanDetailDto>.Fail(
                ServiceErrorCodes.HorseNotFit,
                "Ngựa hiện không đủ điều kiện tập luyện – không thể lập giáo án.",
                fitness.Warnings);
        }

        var tooHeavy = phases.FirstOrDefault(p =>
            !TrainingIntensityLevels.IsWithin(p.IntensityLevel, fitness.MaxAllowedIntensity));
        if (tooHeavy is not null)
        {
            return ServiceResult<TrainingPlanDetailDto>.Fail(
                ServiceErrorCodes.HorseNotFit,
                $"Giai đoạn '{tooHeavy.PhaseName}' có cường độ {tooHeavy.IntensityLevel} vượt mức tối đa theo thể trạng hiện tại ({fitness.MaxAllowedIntensity}).",
                fitness.Warnings);
        }

        return null;
    }

    private static void ApplyPhases(TrainingPlan plan, IReadOnlyList<TrainingPlanPhase> phases)
    {
        foreach (var phase in phases)
        {
            plan.Phases.Add(phase);
        }

        plan.StartDate = phases.Min(p => p.StartDate);
        plan.EndDate = phases.Max(p => p.EndDate);
        plan.IntensityLevel = TrainingIntensityLevels.FromRank(
            phases.Max(p => TrainingIntensityLevels.Rank(p.IntensityLevel)));
    }
}
