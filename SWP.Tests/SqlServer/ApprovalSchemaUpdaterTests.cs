using Microsoft.EntityFrameworkCore;
using SWP.Infrastructure.Persistence;

namespace SWP.Tests.SqlServer;

public class ApprovalSchemaUpdaterTests
{
    [SqlServerFact]
    public void UpgradesExistingSchema_AndCanBeAppliedTwice()
    {
        var server = Environment.GetEnvironmentVariable(SqlServerFactAttribute.EnvironmentVariable)!.TrimEnd(';');
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer($"{server};Database=SWP_ApprovalSchema_{Guid.NewGuid():N}").Options;
        using var db = new ApplicationDbContext(options);
        try
        {
            db.Database.EnsureCreated();
            db.Database.ExecuteSqlRaw("""
                DROP TABLE RaceApproval;
                ALTER TABLE TrainingPlans DROP CONSTRAINT FK_TrainingPlans_DefaultAssignee;
                DROP INDEX IX_TrainingPlans_DefaultAssignedToUserId ON TrainingPlans;
                ALTER TABLE TrainingPlans DROP COLUMN DefaultAssignedToUserId;
                ALTER TABLE TrainingPlans DROP COLUMN ApprovalStatus, ApprovalNote, SentAt, DecidedAt, OwnerSeen, TrainerSeen, ApprovalVersion;
                """);
            ApprovalSchemaUpdater.Apply(db);
            ApprovalSchemaUpdater.Apply(db);
            Assert.Empty(db.TrainingPlans.ToList());
            Assert.Empty(db.RaceApprovals.ToList());
        }
        finally { db.Database.EnsureDeleted(); }
    }
}
