using Microsoft.EntityFrameworkCore;
using SWP.Domain.Common;
using SWP.Domain.Models.Entity;
using SWP.Infrastructure.Persistence;
using SWP.Tests.Support;

namespace SWP.Tests.SqlServer;

/// <summary> Chỉ chạy khi có biến môi trường trỏ tới một SQL Server thật. </summary>
public sealed class SqlServerFactAttribute : FactAttribute
{
    public const string EnvironmentVariable = "SWP_TEST_SQLSERVER";

    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(EnvironmentVariable)))
        {
            Skip = $"Đặt {EnvironmentVariable} = connection string SQL Server (không kèm Database) để chạy.";
        }
    }
}

public class TrainingSchemaUpdaterTests
{
    /// <summary> Đưa DB về schema cũ (trước luồng giáo án) để kiểm tra script bổ sung. </summary>
    private const string RemoveTrainingAdditions = """
        ALTER TABLE [TrainingSchedule] DROP CONSTRAINT [FK_TrainingSchedule_TrainingPlanPhase];
        DROP INDEX [IX_TrainingSchedule_PhaseID] ON [TrainingSchedule];
        ALTER TABLE [TrainingSchedule] DROP COLUMN [PhaseID], [Surface], [ActualDistanceKm], [ActualDurationMinutes],
            [AvgHeartRate], [MaxHeartRate], [AvgSpeedKmh], [TrainerComment];
        ALTER TABLE [TrainingPlans] DROP CONSTRAINT [FK_TrainingPlans_CreatedByUser];
        DROP INDEX [IX_TrainingPlans_CreatedByUserID] ON [TrainingPlans];
        ALTER TABLE [TrainingPlans] DROP COLUMN [CreatedByUserID];
        DROP TABLE [TrainingPlanPhase];
        """;

    [SqlServerFact]
    public void Updater_UpgradesLegacySchema_IsIdempotent_AndSupportsTheTrainingModel()
    {
        var server = Environment.GetEnvironmentVariable(SqlServerFactAttribute.EnvironmentVariable)!.TrimEnd(';');
        var connectionString = $"{server};Database=SWP_TrainingSchema_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connectionString).Options;

        using (var setup = new ApplicationDbContext(options))
        {
            setup.Database.EnsureCreated();
            setup.Database.ExecuteSqlRaw(RemoveTrainingAdditions);
        }

        try
        {
            using (var db = new ApplicationDbContext(options))
            {
                TrainingSchemaUpdater.Apply(db);
                TrainingSchemaUpdater.Apply(db);
            }

            int sessionId;
            using (var db = new ApplicationDbContext(options))
            {
                var ids = TestData.SeedBasics(db);
                var horseId = TestData.AddHorse(db, ids.OwnerId, "SqlServer Horse");
                var (planId, phaseId) = TestData.AddPlan(db, horseId, TestData.Today, TestData.Today.AddDays(13));
                db.TrainingPlans.Single(p => p.PlanId == planId).CreatedByUserId = ids.TrainerId;
                db.SaveChanges();

                var session = new TrainingSchedule
                {
                    PlanId = planId,
                    PhaseId = phaseId,
                    SessionTitle = "Phi nước đại",
                    TrainingDate = TestData.Today.ToDateTime(TimeOnly.MinValue),
                    TrainingType = TrainingTypes.Gallop,
                    Surface = TrackSurfaces.Turf,
                    Status = TrainingSessionStatuses.Completed,
                    ActualDistanceKm = 2.35m,
                    ActualDurationMinutes = 31,
                    AvgHeartRate = 150.5m,
                    MaxHeartRate = 210m,
                    AvgSpeedKmh = 52.25m,
                    PerformanceScore = 9.25m,
                    TrainerComment = "Nhận xét tiếng Việt có dấu: rất tốt",
                    CreatedAt = TestData.Now.UtcDateTime,
                };
                db.TrainingSchedules.Add(session);
                db.SaveChanges();
                sessionId = session.ScheduleId;
            }

            using (var db = new ApplicationDbContext(options))
            {
                var stored = db.TrainingSchedules
                    .Include(s => s.Phase)
                    .Include(s => s.Plan).ThenInclude(p => p.CreatedByUser)
                    .Single(s => s.ScheduleId == sessionId);

                Assert.Equal("Giai đoạn dữ liệu", stored.Phase!.PhaseName);
                Assert.Equal(TrackSurfaces.Turf, stored.Surface);
                Assert.Equal(2.35m, stored.ActualDistanceKm);
                Assert.Equal(31, stored.ActualDurationMinutes);
                Assert.Equal(52.25m, stored.AvgSpeedKmh);
                Assert.Equal("Nhận xét tiếng Việt có dấu: rất tốt", stored.TrainerComment);
                Assert.Equal("HLV Trưởng", stored.Plan.CreatedByUser!.FullName);

                // Xóa giáo án phải xóa dây chuyền các giai đoạn (ON DELETE CASCADE do script tạo).
                db.TrainingSchedules.Remove(stored);
                db.SaveChanges();
                db.Database.ExecuteSqlRaw("DELETE FROM [TrainingPlans] WHERE [PlanID] = {0}", stored.PlanId);
                Assert.Equal(0, db.TrainingPlanPhases.Count());
            }
        }
        finally
        {
            using var cleanup = new ApplicationDbContext(options);
            cleanup.Database.EnsureDeleted();
        }
    }
}
