using Microsoft.EntityFrameworkCore;

namespace SWP.Infrastructure.Persistence;

/// <summary>
/// Bổ sung bảng khóa huấn luyện và lịch tiêm trên SQL Server có sẵn.
/// Idempotent: chạy lại nhiều lần không đổi schema đã đủ.
/// </summary>
public static class VeterinarySchemaUpdater
{
    private static readonly string[] Statements =
    [
        """
        IF COL_LENGTH('Horse', 'LockReason') IS NULL
            ALTER TABLE [Horse] ADD [LockReason] NVARCHAR(500) NULL;
        """,
        """
        IF COL_LENGTH('Horse', 'LockedByUserID') IS NULL
            ALTER TABLE [Horse] ADD [LockedByUserID] INT NULL
                CONSTRAINT [FK_Horse_LockedByUser] REFERENCES [Users]([UserID]);
        """,
        """
        IF COL_LENGTH('Horse', 'LockedAt') IS NULL
            ALTER TABLE [Horse] ADD [LockedAt] DATETIME NULL;
        """,
        """
        IF OBJECT_ID(N'TrainingLock', N'U') IS NULL
        BEGIN
            CREATE TABLE [TrainingLock] (
                [LockID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                [HorseID] INT NOT NULL,
                [VetID] INT NOT NULL,
                [LyDo] NVARCHAR(200) NOT NULL,
                [NgayDat] DATE NOT NULL,
                [NgayGo] DATE NULL,
                [CreatedAt] DATETIME NOT NULL,
                CONSTRAINT [FK_TrainingLock_Horse] FOREIGN KEY ([HorseID]) REFERENCES [Horse]([HorseID]) ON DELETE CASCADE,
                CONSTRAINT [FK_TrainingLock_Vet] FOREIGN KEY ([VetID]) REFERENCES [Users]([UserID])
            );
        END;
        """,
        """
        IF OBJECT_ID(N'VaccinationSchedule', N'U') IS NULL
        BEGIN
            CREATE TABLE [VaccinationSchedule] (
                [ScheduleID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                [HorseID] INT NOT NULL,
                [Loai] NVARCHAR(50) NOT NULL,
                [NgayHen] DATE NOT NULL,
                [TrangThai] NVARCHAR(20) NOT NULL,
                [CreatedAt] DATETIME NOT NULL,
                CONSTRAINT [FK_VaccinationSchedule_Horse] FOREIGN KEY ([HorseID]) REFERENCES [Horse]([HorseID]) ON DELETE CASCADE
            );
        END;
        """,
    ];

    public static void Apply(ApplicationDbContext db)
    {
        foreach (var statement in Statements)
        {
            db.Database.ExecuteSqlRaw(statement);
        }
    }
}
