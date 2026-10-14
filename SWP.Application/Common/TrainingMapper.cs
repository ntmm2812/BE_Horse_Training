using SWP.Application.DTOs.Training;
using SWP.Application.Training;
using SWP.Domain.Common;
using SWP.Domain.Models.Entity;

namespace SWP.Application.Common;

public static class TrainingMapper
{
    /// <param name="plan">Giáo án chứa buổi tập, khi navigation của buổi chưa được load.</param>
    public static TrainingSessionDto ToSessionDto(TrainingSchedule session, TrainingPlan? plan = null)
    {
        plan ??= session.Plan;
        var phase = session.Phase ?? plan?.Phases.FirstOrDefault(p => p.PhaseId == session.PhaseId);

        return new TrainingSessionDto
        {
            SessionId = session.ScheduleId,
            PlanId = session.PlanId,
            PlanName = plan?.PlanName,
            PhaseId = session.PhaseId,
            PhaseName = phase?.PhaseName,
            HorseId = plan?.HorseId ?? 0,
            HorseName = plan?.Horse?.HorseName,
            SessionTitle = session.SessionTitle,
            TrainingDate = DateOnly.FromDateTime(session.TrainingDate),
            StartTime = FormatTime(session.StartTime),
            EndTime = FormatTime(session.EndTime),
            TrainingType = session.TrainingType,
            IntensityLevel = TrainingTypes.GetIntensity(session.TrainingType),
            Surface = session.Surface,
            DistanceKm = session.DistanceKm,
            DurationMinutes = session.DurationMinutes,
            Status = session.Status,
            AssignedToUserId = session.AssignedToUserId,
            AssignedToName = session.AssignedToUser?.FullName,
            Notes = session.Notes,
            ActualDistanceKm = session.ActualDistanceKm,
            ActualDurationMinutes = session.ActualDurationMinutes,
            AvgHeartRate = session.AvgHeartRate,
            MaxHeartRate = session.MaxHeartRate,
            AvgSpeedKmh = session.AvgSpeedKmh,
            PerformanceScore = session.PerformanceScore,
            Result = session.Result,
            TrainerComment = session.TrainerComment,
            TrialVideoUrl = session.TrialVideoUrl,
            CreatedAt = session.CreatedAt,
        };
    }

    public static TrainingSessionDto ToPreviewDto(GeneratedSession generated, TrainingPlan plan, int? assignedToUserId)
    {
        return new TrainingSessionDto
        {
            PlanId = plan.PlanId,
            PlanName = plan.PlanName,
            PhaseId = generated.PhaseId,
            PhaseName = generated.PhaseName,
            HorseId = plan.HorseId,
            HorseName = plan.Horse?.HorseName,
            SessionTitle = generated.Title,
            TrainingDate = generated.Date,
            StartTime = generated.StartTime.ToString("HH:mm"),
            TrainingType = generated.TrainingType,
            IntensityLevel = generated.IntensityLevel,
            Surface = generated.Surface,
            DistanceKm = generated.DistanceKm,
            DurationMinutes = generated.DurationMinutes,
            Status = TrainingSessionStatuses.Scheduled,
            AssignedToUserId = assignedToUserId,
        };
    }

    public static TrainingPhaseDto ToPhaseDto(TrainingPlanPhase phase)
    {
        return new TrainingPhaseDto
        {
            PhaseId = phase.PhaseId,
            PhaseOrder = phase.PhaseOrder,
            PhaseName = phase.PhaseName,
            Goal = phase.Goal,
            StartDate = phase.StartDate,
            EndDate = phase.EndDate,
            IntensityLevel = phase.IntensityLevel,
            SessionsPerWeek = phase.SessionsPerWeek,
            TargetDistanceKm = phase.TargetDistanceKm,
            SessionDurationMinutes = phase.SessionDurationMinutes,
            Surface = phase.Surface,
        };
    }

    public static TrainingPlanDto ToPlanDto(TrainingPlan plan)
    {
        var dto = new TrainingPlanDto();
        FillPlan(dto, plan);
        return dto;
    }

    public static TrainingPlanDetailDto ToPlanDetailDto(TrainingPlan plan)
    {
        var dto = new TrainingPlanDetailDto
        {
            Phases = plan.Phases.OrderBy(p => p.PhaseOrder).Select(ToPhaseDto).ToList(),
            Sessions = plan.TrainingSchedules
                .OrderBy(s => s.TrainingDate)
                .ThenBy(s => s.StartTime)
                .Select(s => ToSessionDto(s, plan))
                .ToList(),
        };
        FillPlan(dto, plan);
        return dto;
    }

    public static TrainingPlanProgressDto ToProgressDto(IEnumerable<TrainingSchedule> sessions)
    {
        var list = sessions.ToList();
        var completed = list.Where(s => TrainingSessionStatuses.Is(s.Status, TrainingSessionStatuses.Completed)).ToList();
        var cancelled = list.Count(s => TrainingSessionStatuses.Is(s.Status, TrainingSessionStatuses.Cancelled));
        var countable = list.Count - cancelled;
        var scores = completed.Where(s => s.PerformanceScore.HasValue).Select(s => s.PerformanceScore!.Value).ToList();

        return new TrainingPlanProgressDto
        {
            TotalSessions = list.Count,
            CompletedSessions = completed.Count,
            CancelledSessions = cancelled,
            UpcomingSessions = list.Count(s => TrainingSessionStatuses.Is(s.Status, TrainingSessionStatuses.Scheduled)
                                               || TrainingSessionStatuses.Is(s.Status, TrainingSessionStatuses.InProgress)),
            CompletionRate = countable == 0 ? 0 : Math.Round(completed.Count * 100m / countable, 2),
            AveragePerformanceScore = scores.Count == 0 ? null : Math.Round(scores.Average(), 2),
            TotalActualDistanceKm = completed.Sum(s => s.ActualDistanceKm ?? s.DistanceKm ?? 0),
        };
    }

    public static HorseFitnessAssessmentDto ToAssessmentDto(
        Horse horse,
        FitnessInput input,
        HealthMetric? vitals,
        RecentTrainingLoadDto recentLoad,
        FitnessEvaluation evaluation,
        DateTime assessedAt)
    {
        return new HorseFitnessAssessmentDto
        {
            HorseId = horse.HorseId,
            HorseName = horse.HorseName,
            AgeYears = input.AgeYears,
            WeightKg = horse.Weight,
            HealthStatus = input.HealthStatus,
            IsTrainingLocked = input.IsTrainingLocked,
            LockReason = input.IsTrainingLocked ? horse.LockReason : null,
            LatestVitals = vitals is null
                ? null
                : new FitnessVitalsDto
                {
                    HeartRate = vitals.HeartRate,
                    RespiratoryRate = vitals.RespiratoryRate,
                    BodyTemperature = vitals.BodyTemperature,
                    RecordedAt = vitals.RecordedAt,
                },
            RecentLoad = recentLoad,
            Readiness = evaluation.Readiness,
            ReadinessLabel = FitnessReadiness.Labels[evaluation.Readiness],
            CanTrain = evaluation.CanTrain,
            MaxAllowedIntensity = evaluation.MaxAllowedIntensity,
            RecommendedIntensity = evaluation.RecommendedIntensity,
            AllowedTrainingTypes = evaluation.MaxAllowedIntensity is null
                ? Array.Empty<string>()
                : TrainingTypes.All
                    .Where(t => TrainingIntensityLevels.IsWithin(TrainingTypes.GetIntensity(t), evaluation.MaxAllowedIntensity))
                    .ToList(),
            Warnings = evaluation.Warnings,
            AssessedAt = assessedAt,
        };
    }

    public static TrainingOptionsDto BuildOptions()
    {
        static IReadOnlyList<TrainingOptionDto> FromLabels(IEnumerable<string> codes, IReadOnlyDictionary<string, string> labels) =>
            codes.Select(c => new TrainingOptionDto { Code = c, Label = labels[c] }).ToList();

        return new TrainingOptionsDto
        {
            IntensityLevels = FromLabels(TrainingIntensityLevels.All, TrainingIntensityLevels.Labels),
            TrainingTypes = TrainingTypes.All
                .Select(t => new TrainingOptionDto
                {
                    Code = t,
                    Label = TrainingTypes.Labels[t],
                    IntensityLevel = TrainingTypes.GetIntensity(t),
                    DefaultSurface = TrainingTypes.GetDefaultSurface(t),
                })
                .ToList(),
            Surfaces = FromLabels(TrackSurfaces.All, TrackSurfaces.Labels),
            PlanTypes = FromLabels(TrainingPlanTypes.All, TrainingPlanTypes.Labels),
            PlanStatuses = FromLabels(TrainingPlanStatuses.All, TrainingPlanStatuses.Labels),
            SessionStatuses = FromLabels(TrainingSessionStatuses.All, TrainingSessionStatuses.Labels),
        };
    }

    public static string? FormatTime(DateTime? value) => value?.ToString("HH:mm");

    private static void FillPlan(TrainingPlanDto dto, TrainingPlan plan)
    {
        dto.PlanId = plan.PlanId;
        dto.PlanName = plan.PlanName;
        dto.HorseId = plan.HorseId;
        dto.HorseName = plan.Horse?.HorseName;
        dto.Objective = plan.Objective;
        dto.TrainingType = plan.TrainingType;
        dto.IntensityLevel = plan.IntensityLevel;
        dto.Status = plan.PlanStatus;
        dto.StartDate = plan.StartDate;
        dto.EndDate = plan.EndDate;
        dto.Notes = plan.Notes;
        dto.CreatedByUserId = plan.CreatedByUserId;
        dto.CreatedAt = plan.CreatedAt;
        dto.UpdatedAt = plan.UpdatedAt;
        dto.PhaseCount = plan.Phases.Count;
        dto.Progress = ToProgressDto(plan.TrainingSchedules);
    }
}
