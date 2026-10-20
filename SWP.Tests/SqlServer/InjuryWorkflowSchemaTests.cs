using Microsoft.EntityFrameworkCore;
using SWP.Application.DTOs.Medical;
using SWP.Application.Services.Implement;
using SWP.Domain.Models.Entity;
using SWP.Infrastructure.Persistence;
using SWP.Tests.Support;

namespace SWP.Tests.SqlServer;

public class InjuryWorkflowSchemaTests
{
    [SqlServerFact]
    public async Task UpgradeLegacyDatabase_PreservesCareTasks_AndSupportsRealWorkflow()
    {
        var server = Environment.GetEnvironmentVariable(SqlServerFactAttribute.EnvironmentVariable)!.TrimEnd(';');
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer($"{server};Database=SWP_InjuryWorkflow_{Guid.NewGuid():N}").Options;
        using var db = new ApplicationDbContext(options);
        try
        {
            db.Database.EnsureCreated();
            var ids = TestData.SeedBasics(db);
            var horseId = TestData.AddHorse(db, ids.OwnerId, "SQL injury horse");
            db.CareTasks.Add(new CareTask { HorseId = horseId, UserId = ids.GroomId, TaskName = "Việc chăm sóc cũ",
                Status = "Pending", CreatedAt = TestData.Now.UtcDateTime });
            db.SaveChanges();
            db.Database.ExecuteSqlRaw("""
                ALTER TABLE CareTask DROP CONSTRAINT FK_CareTask_InjuryCase_InjuryCaseId, FK_CareTask_Users_AssignedByUserId;
                ALTER TABLE TrainingLock DROP CONSTRAINT FK_TrainingLock_InjuryCase_InjuryCaseId;
                DROP INDEX IX_CareTask_InjuryCaseId_Status_ScheduledAt ON CareTask;
                DROP INDEX IX_CareTask_AssignedByUserId ON CareTask;
                DROP INDEX IX_TrainingLock_InjuryCaseId ON TrainingLock;
                DECLARE @defaultName SYSNAME = (SELECT dc.name FROM sys.default_constraints dc
                    JOIN sys.columns c ON c.object_id=dc.parent_object_id AND c.column_id=dc.parent_column_id
                    WHERE dc.parent_object_id=OBJECT_ID(N'CareTask') AND c.name=N'Version');
                EXEC ('ALTER TABLE CareTask DROP CONSTRAINT ' + @defaultName);
                ALTER TABLE CareTask DROP COLUMN InjuryCaseId, AssignedByUserId, Version;
                ALTER TABLE TrainingLock DROP COLUMN InjuryCaseId;
                DROP TABLE InjuryCaseEvent;
                DROP TABLE InjuryCase;
                """);
            InjuryWorkflowSchemaUpdater.Apply(db);
            InjuryWorkflowSchemaUpdater.Apply(db);
            db.ChangeTracker.Clear();
            var oldTask = db.CareTasks.AsNoTracking().Single();
            Assert.Equal("Việc chăm sóc cũ", oldTask.TaskName);
            Assert.Null(oldTask.InjuryCaseId);
            Assert.Equal(1, oldTask.Version);
            Assert.Empty(db.InjuryCases.AsNoTracking());
            Assert.Empty(db.InjuryCaseEvents.AsNoTracking());
            var service = new InjuryWorkflowService(db, new FixedTimeProvider(TestData.Now));
            var injury = await service.CreateCase(ids.VetId, new OpenInjuryCaseRequest
            {
                HorseId = horseId, Title = "Hồ sơ thử trên SQL Server", BodyRegion = "Chân trước",
                ClinicalNotes = "Ghi nhận kiểm thử", OwnerSummary = "Đang theo dõi", TrainingGuidance = "Chờ đánh giá vận động"
            }, default);
            var task = await service.AssignTask(ids.VetId, injury.Id, new AssignMedicalCareRequest
            {
                Version = injury.Version, GroomId = ids.GroomId, Title = "Chăm sóc theo chỉ định",
                Instructions = "Ghi lại quan sát theo hướng dẫn", DueAt = TestData.Now.AddHours(3)
            }, default);
            Assert.Equal(injury.Id, task.InjuryCaseId);
            Assert.Equal(2, db.InjuryCaseEvents.Count());
            Assert.Equal(2, db.CareTasks.Count());
            Assert.Equal(11, db.Database.SqlQueryRaw<int>("""
                SELECT COUNT(*) AS [Value] FROM sys.extended_properties WHERE name=N'MS_Description'
                AND major_id IN (OBJECT_ID(N'InjuryCase'), OBJECT_ID(N'InjuryCaseEvent'), OBJECT_ID(N'CareTask'), OBJECT_ID(N'TrainingLock'))
                """).Single());
        }
        finally { db.Database.EnsureDeleted(); }
    }
}
