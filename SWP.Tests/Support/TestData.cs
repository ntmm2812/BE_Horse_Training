using SWP.Application.DTOs.Training;
using SWP.Domain.Common;
using SWP.Domain.Models.Entity;
using SWP.Infrastructure.Persistence;

namespace SWP.Tests.Support;

public sealed record SeededIds(
    int TrainerId,
    int GroomId,
    int PendingGroomId,
    int OwnerId,
    int VetId,
    int ManagerId);

/// <summary> Dữ liệu mẫu dùng chung cho test service và test API. </summary>
public static class TestData
{
    /// <summary> Thứ Hai 12/10/2026 08:00 (UTC). </summary>
    public static readonly DateTimeOffset Now = new(2026, 10, 12, 8, 0, 0, TimeSpan.Zero);

    public static DateOnly Today => DateOnly.FromDateTime(Now.UtcDateTime);

    /// <summary> Thứ Hai tuần sau. </summary>
    public static DateOnly NextMonday => Today.AddDays(7);

    public static SeededIds SeedBasics(ApplicationDbContext db)
    {
        var roles = new Dictionary<string, Role>
        {
            ["Head Trainer"] = new() { RoleName = "Head Trainer" },
            ["Groom"] = new() { RoleName = "Groom" },
            ["Horse Owner"] = new() { RoleName = "Horse Owner" },
            ["Veterinarian"] = new() { RoleName = "Veterinarian" },
            ["Club Manager"] = new() { RoleName = "Club Manager" },
        };
        db.Roles.AddRange(roles.Values);

        User NewUser(string name, string email, string role, string status = UserStatuses.Active) => new()
        {
            FullName = name,
            Email = email,
            Status = status,
            Role = roles[role],
            CreatedAt = Now.UtcDateTime,
        };

        var trainer = NewUser("HLV Trưởng", "trainer@test.vn", "Head Trainer");
        var groom = NewUser("Nhân viên Chăm sóc", "groom@test.vn", "Groom");
        var pendingGroom = NewUser("Nhân viên Chờ duyệt", "pending@test.vn", "Groom", UserStatuses.Pending);
        var owner = NewUser("Chủ Ngựa", "owner@test.vn", "Horse Owner");
        var vet = NewUser("Bác sĩ Thú y", "vet@test.vn", "Veterinarian");
        var manager = NewUser("Quản lý CLB", "manager@test.vn", "Club Manager");
        db.Users.AddRange(trainer, groom, pendingGroom, owner, vet, manager);
        db.SaveChanges();

        return new SeededIds(trainer.UserId, groom.UserId, pendingGroom.UserId, owner.UserId, vet.UserId, manager.UserId);
    }

    public static int AddHorse(
        ApplicationDbContext db,
        int ownerId,
        string name,
        int ageYears = 5,
        string? healthStatus = HorseHealthStatuses.DuDieuKien,
        string? lockReason = null,
        decimal weight = 480m)
    {
        var horse = new Horse
        {
            HorseName = name,
            Breed = "Thoroughbred",
            DateOfBirth = Today.AddYears(-ageYears).AddDays(-30),
            Weight = weight,
            HorseStatus = "Active",
            UserId = ownerId,
            CreatedAt = Now.UtcDateTime,
        };

        if (lockReason is not null)
        {
            horse.LockReason = lockReason;
            horse.LockedAt = Now.UtcDateTime.AddDays(-1);
        }

        db.Horses.Add(horse);
        db.SaveChanges();

        if (healthStatus is not null)
        {
            db.HorseHealthStatuses.Add(new HorseHealthStatus
            {
                HorseId = horse.HorseId,
                TrangThai = healthStatus,
                NgayCapNhat = Now.UtcDateTime,
            });
            db.SaveChanges();
        }

        return horse.HorseId;
    }

    public static void SetHealthStatus(ApplicationDbContext db, int horseId, string status)
    {
        var record = db.HorseHealthStatuses.Single(s => s.HorseId == horseId);
        record.TrangThai = status;
        db.SaveChanges();
    }

    public static void LockHorse(ApplicationDbContext db, int horseId, string reason)
    {
        var horse = db.Horses.Single(h => h.HorseId == horseId);
        horse.LockReason = reason;
        horse.LockedAt = Now.UtcDateTime;
        db.SaveChanges();
    }

    public static void AddMetric(
        ApplicationDbContext db,
        int horseId,
        DateTime recordedAtUtc,
        decimal? heartRate = null,
        decimal? temperature = null,
        decimal? respiratoryRate = null,
        decimal? speedKmh = null)
    {
        db.HealthMetrics.Add(new HealthMetric
        {
            HorseId = horseId,
            RecordedAt = recordedAtUtc,
            HeartRate = heartRate,
            BodyTemperature = temperature,
            RespiratoryRate = respiratoryRate,
            SpeedKmh = speedKmh,
            DataSource = "Test",
        });
        db.SaveChanges();
    }

    /// <summary> Thêm trực tiếp một buổi tập vào DB (bỏ qua validate) để dựng tình huống. </summary>
    public static int AddSession(
        ApplicationDbContext db,
        int planId,
        DateOnly date,
        string trainingType,
        string status,
        decimal? distanceKm = 3m,
        decimal? actualDistanceKm = null,
        decimal? performanceScore = null,
        int? phaseId = null,
        int? assignedToUserId = null)
    {
        var session = new TrainingSchedule
        {
            PlanId = planId,
            PhaseId = phaseId,
            TrainingDate = date.ToDateTime(TimeOnly.MinValue),
            StartTime = date.ToDateTime(new TimeOnly(6, 0)),
            TrainingType = trainingType,
            DistanceKm = distanceKm,
            DurationMinutes = 40,
            ActualDistanceKm = actualDistanceKm,
            PerformanceScore = performanceScore,
            Status = status,
            AssignedToUserId = assignedToUserId,
            CreatedAt = Now.UtcDateTime,
        };
        db.TrainingSchedules.Add(session);
        db.SaveChanges();
        return session.ScheduleId;
    }

    /// <summary> Giáo án lưu thẳng DB với 1 giai đoạn – dùng khi cần dữ liệu lịch sử/quá khứ. </summary>
    public static (int PlanId, int PhaseId) AddPlan(
        ApplicationDbContext db,
        int horseId,
        DateOnly start,
        DateOnly end,
        string status = TrainingPlanStatuses.Active,
        string intensity = TrainingIntensityLevels.High)
    {
        var phase = new TrainingPlanPhase
        {
            PhaseOrder = 1,
            PhaseName = "Giai đoạn dữ liệu",
            StartDate = start,
            EndDate = end,
            IntensityLevel = intensity,
            SessionsPerWeek = 5,
            TargetDistanceKm = 3m,
            SessionDurationMinutes = 40,
            CreatedAt = Now.UtcDateTime,
        };
        var plan = new TrainingPlan
        {
            HorseId = horseId,
            PlanName = "Giáo án dữ liệu",
            StartDate = start,
            EndDate = end,
            IntensityLevel = intensity,
            PlanStatus = status,
            CreatedAt = Now.UtcDateTime,
            Phases = { phase },
        };
        db.TrainingPlans.Add(plan);
        db.SaveChanges();
        return (plan.PlanId, phase.PhaseId);
    }

    public static TrainingPhaseRequestDto Phase(
        string name,
        DateOnly start,
        DateOnly end,
        string intensity,
        int sessionsPerWeek = 5,
        decimal distanceKm = 3m,
        int durationMinutes = 40,
        string? surface = null) => new()
    {
        PhaseName = name,
        Goal = $"Mục tiêu {name}",
        StartDate = start,
        EndDate = end,
        IntensityLevel = intensity,
        SessionsPerWeek = sessionsPerWeek,
        TargetDistanceKm = distanceKm,
        SessionDurationMinutes = durationMinutes,
        Surface = surface,
    };

    /// <summary> Giáo án 3 giai đoạn x 2 tuần bắt đầu hôm nay (Thứ Hai): Low → Medium → High. </summary>
    public static CreateTrainingPlanRequestDto ThreePhasePlan(int horseId, string name = "Giáo án chuẩn bị giải") => new()
    {
        HorseId = horseId,
        PlanName = name,
        Objective = "Chuẩn bị cho giải đua tháng 12",
        TrainingType = TrainingPlanTypes.RacePreparation,
        Notes = "Theo dõi nhịp tim sau mỗi buổi phi nước đại",
        Phases =
        [
            Phase("Nền tảng", Today, Today.AddDays(13), TrainingIntensityLevels.Low, 4, 3m, 35, TrackSurfaces.Sand),
            Phase("Phát triển", Today.AddDays(14), Today.AddDays(27), TrainingIntensityLevels.Medium, 5, 3.5m, 40, TrackSurfaces.Dirt),
            Phase("Tăng tốc", Today.AddDays(28), Today.AddDays(41), TrainingIntensityLevels.High, 5, 2m, 30, TrackSurfaces.Turf),
        ],
    };

    public static SaveTrainingSessionRequestDto Session(
        DateOnly date,
        string trainingType,
        decimal distanceKm = 3m,
        int durationMinutes = 40,
        string? surface = null,
        string? startTime = null,
        int? assignedToUserId = null) => new()
    {
        TrainingDate = date,
        TrainingType = trainingType,
        DistanceKm = distanceKm,
        DurationMinutes = durationMinutes,
        Surface = surface,
        StartTime = startTime,
        AssignedToUserId = assignedToUserId,
    };

    public static CompleteTrainingSessionRequestDto Completion(
        decimal score = 8.5m,
        string comment = "Ngựa giữ nhịp tốt, hồi sức nhanh.") => new()
    {
        ActualDistanceKm = 3.2m,
        ActualDurationMinutes = 42,
        PerformanceScore = score,
        AvgHeartRate = 120m,
        MaxHeartRate = 185m,
        AvgSpeedKmh = 28.5m,
        Result = "Hoàn thành tốt",
        TrainerComment = comment,
        TrialVideoUrl = "https://videos.example.com/trial-1.mp4",
    };
}
