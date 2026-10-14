using Microsoft.EntityFrameworkCore;

namespace SWP.Infrastructure.Persistence;

/// <summary>
/// Bổ sung bảng/cột cho luồng Lập &amp; Thực hiện Giáo án trên database SQL Server có sẵn.
/// Idempotent: chạy lại nhiều lần không thay đổi gì nếu schema đã đủ.
/// </summary>
public static class TrainingSchemaUpdater
{
    private static readonly string[] Statements =
    [
        """
        IF OBJECT_ID(N'TrainingPlanPhase', N'U') IS NULL
        BEGIN
            CREATE TABLE [TrainingPlanPhase] (
                [PhaseID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                [PlanID] INT NOT NULL,
                [PhaseOrder] INT NOT NULL,
                [PhaseName] NVARCHAR(100) NOT NULL,
                [Goal] NVARCHAR(500) NULL,
                [StartDate] DATE NOT NULL,
                [EndDate] DATE NOT NULL,
                [IntensityLevel] VARCHAR(20) NOT NULL,
                [SessionsPerWeek] INT NOT NULL,
                [TargetDistanceKm] DECIMAL(8, 2) NOT NULL,
                [SessionDurationMinutes] INT NOT NULL,
                [Surface] VARCHAR(30) NULL,
                [CreatedAt] DATETIME NOT NULL,
                CONSTRAINT [FK_TrainingPlanPhase_TrainingPlans] FOREIGN KEY ([PlanID])
                    REFERENCES [TrainingPlans]([PlanID]) ON DELETE CASCADE
            );
        END;
        """,
        """
        IF COL_LENGTH('TrainingPlans', 'CreatedByUserID') IS NULL
            ALTER TABLE [TrainingPlans] ADD [CreatedByUserID] INT NULL
                CONSTRAINT [FK_TrainingPlans_CreatedByUser] REFERENCES [Users]([UserID]);
        """,
        """
        IF COL_LENGTH('TrainingSchedule', 'PhaseID') IS NULL
            ALTER TABLE [TrainingSchedule] ADD [PhaseID] INT NULL
                CONSTRAINT [FK_TrainingSchedule_TrainingPlanPhase] REFERENCES [TrainingPlanPhase]([PhaseID]);
        """,
        """
        IF COL_LENGTH('TrainingSchedule', 'Surface') IS NULL
            ALTER TABLE [TrainingSchedule] ADD [Surface] VARCHAR(30) NULL;

        IF COL_LENGTH('TrainingSchedule', 'ActualDistanceKm') IS NULL
            ALTER TABLE [TrainingSchedule] ADD [ActualDistanceKm] DECIMAL(8, 2) NULL;

        IF COL_LENGTH('TrainingSchedule', 'ActualDurationMinutes') IS NULL
            ALTER TABLE [TrainingSchedule] ADD [ActualDurationMinutes] INT NULL;

        IF COL_LENGTH('TrainingSchedule', 'AvgHeartRate') IS NULL
            ALTER TABLE [TrainingSchedule] ADD [AvgHeartRate] DECIMAL(6, 2) NULL;

        IF COL_LENGTH('TrainingSchedule', 'MaxHeartRate') IS NULL
            ALTER TABLE [TrainingSchedule] ADD [MaxHeartRate] DECIMAL(6, 2) NULL;

        IF COL_LENGTH('TrainingSchedule', 'AvgSpeedKmh') IS NULL
            ALTER TABLE [TrainingSchedule] ADD [AvgSpeedKmh] DECIMAL(6, 2) NULL;

        IF COL_LENGTH('TrainingSchedule', 'TrainerComment') IS NULL
            ALTER TABLE [TrainingSchedule] ADD [TrainerComment] NVARCHAR(1000) NULL;
        """,
    ];

    public static void Apply(ApplicationDbContext db)
    {
        // Mỗi câu lệnh là một batch riêng để bảng/cột vừa tạo được nhìn thấy ở batch sau.
        foreach (var statement in Statements)
        {
            db.Database.ExecuteSqlRaw(statement);
        }
    }
}
