using Microsoft.EntityFrameworkCore;

namespace SWP.Infrastructure.Persistence;

/// <summary>
/// Bổ sung bảng/cột cho luồng Y tế, Bệnh án, Lệnh khóa huấn luyện & Lịch tiêm phòng trên SQL Server.
/// Idempotent: chạy lại nhiều lần an toàn mà không làm mất dữ liệu.
/// </summary>
public static class MedicalSchemaUpdater
{
    private static readonly string[] Statements =
    [
        """
        IF COL_LENGTH('MedicalRecord', 'ViTriChanThuong') IS NULL
            ALTER TABLE [MedicalRecord] ADD [ViTriChanThuong] NVARCHAR(255) NULL;
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
                CONSTRAINT [FK_TrainingLock_Horse] FOREIGN KEY ([HorseID]) REFERENCES [Horse]([HorseID]) ON DELETE CASCADE,
                CONSTRAINT [FK_TrainingLock_Users] FOREIGN KEY ([VetID]) REFERENCES [Users]([UserID])
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
                [TrangThai] NVARCHAR(30) NOT NULL,
                [GhiChu] NVARCHAR(500) NULL,
                CONSTRAINT [FK_VaccinationSchedule_Horse] FOREIGN KEY ([HorseID]) REFERENCES [Horse]([HorseID]) ON DELETE CASCADE
            );
        END;
        """
    ];

    public static void Apply(ApplicationDbContext db)
    {
        using var transaction = db.Database.BeginTransaction();
        foreach (var statement in Statements)
        {
            db.Database.ExecuteSqlRaw(statement);
        }
        transaction.Commit();
    }
}
