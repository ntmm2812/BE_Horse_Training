using Microsoft.EntityFrameworkCore;

namespace SWP.Infrastructure.Persistence;

public static class TrainingReportSchemaUpdater
{
    public static void Apply(ApplicationDbContext db) => db.Database.ExecuteSqlRaw(Sql);

    public const string Sql = """
        IF OBJECT_ID(N'TrainingSessionReport', N'U') IS NULL
        BEGIN
            CREATE TABLE TrainingSessionReport (
                Id INT IDENTITY PRIMARY KEY,
                ScheduleId INT NOT NULL CONSTRAINT UQ_TrainingSessionReport_ScheduleId UNIQUE
                    CONSTRAINT FK_TrainingSessionReport_TrainingSchedule REFERENCES TrainingSchedule(ScheduleID) ON DELETE CASCADE,
                Result NVARCHAR(20) NOT NULL,
                ActualDistanceKm DECIMAL(8, 2) NULL,
                HorseReaction NVARCHAR(20) NOT NULL,
                Note NVARCHAR(300) NOT NULL,
                ReportedByUserId INT NOT NULL CONSTRAINT FK_TrainingSessionReport_Users REFERENCES Users(UserID),
                ReportedAt DATETIME2 NOT NULL
            );
        END;
        """;
}
