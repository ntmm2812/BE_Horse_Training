using Microsoft.EntityFrameworkCore;

namespace SWP.Infrastructure.Persistence;

public static class ApprovalSchemaUpdater
{
    public static void Apply(ApplicationDbContext db) => db.Database.ExecuteSqlRaw(Sql);

    public const string Sql = """
        IF COL_LENGTH('TrainingPlans', 'DefaultAssignedToUserId') IS NULL
        BEGIN
            ALTER TABLE TrainingPlans ADD DefaultAssignedToUserId INT NULL;
            ALTER TABLE TrainingPlans ADD CONSTRAINT FK_TrainingPlans_DefaultAssignee FOREIGN KEY (DefaultAssignedToUserId) REFERENCES Users(UserID);
            CREATE INDEX IX_TrainingPlans_DefaultAssignedToUserId ON TrainingPlans(DefaultAssignedToUserId);
        END;
        IF COL_LENGTH('TrainingPlans', 'ApprovalStatus') IS NULL
        BEGIN
            ALTER TABLE TrainingPlans ADD ApprovalStatus NVARCHAR(30) NOT NULL CONSTRAINT DF_PlanApprovalStatus DEFAULT 'Draft';
            ALTER TABLE TrainingPlans ADD ApprovalNote NVARCHAR(300) NOT NULL CONSTRAINT DF_PlanApprovalNote DEFAULT '';
            ALTER TABLE TrainingPlans ADD SentAt DATETIME2 NULL, DecidedAt DATETIME2 NULL;
            ALTER TABLE TrainingPlans ADD OwnerSeen BIT NOT NULL CONSTRAINT DF_PlanOwnerSeen DEFAULT 1;
            ALTER TABLE TrainingPlans ADD TrainerSeen BIT NOT NULL CONSTRAINT DF_PlanTrainerSeen DEFAULT 1;
            ALTER TABLE TrainingPlans ADD ApprovalVersion NVARCHAR(36) NOT NULL CONSTRAINT DF_PlanApprovalVersion DEFAULT (CONVERT(NVARCHAR(36), NEWID()));
        END;
        IF OBJECT_ID(N'RaceApproval', N'U') IS NULL
        BEGIN
            CREATE TABLE RaceApproval (
                Id INT IDENTITY PRIMARY KEY,
                HorseId INT NOT NULL REFERENCES Horse(HorseID),
                RaceId INT NOT NULL REFERENCES Race(RaceID),
                TrainerId INT NOT NULL REFERENCES Users(UserID),
                Status NVARCHAR(30) NOT NULL,
                Note NVARCHAR(300) NOT NULL,
                RejectedBy NVARCHAR(20) NULL,
                RejectReason NVARCHAR(300) NOT NULL,
                TrainerSeen BIT NOT NULL,
                CreatedAt DATETIME2 NOT NULL,
                SubmittedAt DATETIME2 NULL,
                HistoryJson NVARCHAR(MAX) NOT NULL,
                Version NVARCHAR(36) NOT NULL
            );
            CREATE INDEX IX_RaceApproval_HorseId_RaceId ON RaceApproval(HorseId, RaceId);
        END;
        """;
}
