using Microsoft.EntityFrameworkCore;

namespace SWP.Infrastructure.Persistence;

public static class TrainingReviewSchemaUpdater
{
    public static void Apply(ApplicationDbContext db) => db.Database.ExecuteSqlRaw(Sql);

    public const string Sql = """
        IF OBJECT_ID(N'TrainingSessionReview', N'U') IS NULL
        BEGIN
            CREATE TABLE TrainingSessionReview (
                Id INT IDENTITY PRIMARY KEY,
                ScheduleId INT NOT NULL CONSTRAINT UQ_TrainingSessionReview_ScheduleId UNIQUE
                    CONSTRAINT FK_TrainingSessionReview_TrainingSchedule REFERENCES TrainingSchedule(ScheduleID) ON DELETE CASCADE,
                Score INT NOT NULL,
                Assessment NVARCHAR(20) NOT NULL,
                Comment NVARCHAR(500) NOT NULL,
                ReviewedByUserId INT NOT NULL CONSTRAINT FK_TrainingSessionReview_Users REFERENCES Users(UserID),
                ReviewedAt DATETIME2 NOT NULL,
                SentToOwnerAt DATETIME2 NULL
            );
        END;
        """;
}
