using SWP.Application.Common;
using SWP.Application.DTOs.Training;
using SWP.Application.Services.Interface;
using SWP.Application.Training;
using SWP.Domain.Common;
using SWP.Infrastructure.Repositories.Interface;

namespace SWP.Application.Services.Implement;

public class HorseFitnessService : IHorseFitnessService
{
    private readonly IHorseRepository _horseRepository;
    private readonly IHorseHealthStatusRepository _horseHealthStatusRepository;
    private readonly ITrainingSessionRepository _trainingSessionRepository;
    private readonly TimeProvider _timeProvider;

    public HorseFitnessService(
        IHorseRepository horseRepository,
        IHorseHealthStatusRepository horseHealthStatusRepository,
        ITrainingSessionRepository trainingSessionRepository,
        TimeProvider timeProvider)
    {
        _horseRepository = horseRepository;
        _horseHealthStatusRepository = horseHealthStatusRepository;
        _trainingSessionRepository = trainingSessionRepository;
        _timeProvider = timeProvider;
    }

    public async Task<HorseFitnessAssessmentDto?> GetAssessmentAsync(
        int horseId,
        CancellationToken cancellationToken = default)
    {
        var horse = await _horseRepository.GetByIdAsync(horseId, cancellationToken);
        if (horse is null)
        {
            return null;
        }

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);

        var health = await _horseHealthStatusRepository.GetByHorseIdAsync(horseId, cancellationToken);
        var vitals = await _trainingSessionRepository.GetLatestRestingMetricAsync(
            horseId,
            utcNow.AddDays(-TrainingRules.VitalsWindowDays),
            cancellationToken);
        var recentSessions = await _trainingSessionRepository.GetCompletedSinceAsync(
            horseId,
            today.AddDays(-TrainingRules.LoadWindowDays),
            cancellationToken);

        var highIntensity = recentSessions.Count(s =>
            TrainingTypes.GetIntensity(s.TrainingType) == TrainingIntensityLevels.High);
        var recentLoad = new RecentTrainingLoadDto
        {
            CompletedSessions = recentSessions.Count,
            HighIntensitySessions = highIntensity,
            DistanceKm = recentSessions.Sum(s => s.ActualDistanceKm ?? s.DistanceKm ?? 0),
        };

        var input = new FitnessInput
        {
            AgeYears = CalculateAge(horse.DateOfBirth, today),
            HealthStatus = health?.TrangThai,
            // Lệnh "Khóa huấn luyện" của bác sĩ thú y được ghi vào Horse.LockedAt/LockReason.
            IsTrainingLocked = horse.LockedAt.HasValue,
            LockReason = horse.LockReason,
            RestingHeartRate = vitals?.HeartRate,
            RespiratoryRate = vitals?.RespiratoryRate,
            BodyTemperature = vitals?.BodyTemperature,
            HighIntensitySessionsLast7Days = highIntensity,
        };

        var evaluation = FitnessEvaluator.Evaluate(input);
        return TrainingMapper.ToAssessmentDto(horse, input, vitals, recentLoad, evaluation, utcNow);
    }

    public static int? CalculateAge(DateOnly? dateOfBirth, DateOnly today)
    {
        if (dateOfBirth is not { } dob || dob > today)
        {
            return null;
        }

        // So (tháng, ngày) thay vì AddYears: ngựa sinh 29/2 chỉ tròn tuổi từ 1/3 ở năm không nhuận.
        var age = today.Year - dob.Year;
        if ((today.Month, today.Day).CompareTo((dob.Month, dob.Day)) < 0)
        {
            age--;
        }

        return age;
    }
}
