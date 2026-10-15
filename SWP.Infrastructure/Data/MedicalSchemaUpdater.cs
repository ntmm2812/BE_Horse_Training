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
        """,
        """
        IF NOT EXISTS (SELECT 1 FROM [TrainingLock])
        BEGIN
            IF EXISTS (SELECT 1 FROM [Horse] WHERE [HorseID] = 4)
            BEGIN
                INSERT INTO [TrainingLock] ([HorseID], [VetID], [LyDo], [NgayDat], [NgayGo])
                VALUES (4, 2, N'Viêm gân nhẹ chân trước', DATEADD(day, -20, CAST(GETDATE() AS DATE)), DATEADD(day, -12, CAST(GETDATE() AS DATE)));
            END;

            IF EXISTS (SELECT 1 FROM [Horse] WHERE [HorseID] = 3)
            BEGIN
                INSERT INTO [TrainingLock] ([HorseID], [VetID], [LyDo], [NgayDat], [NgayGo])
                VALUES (3, 2, N'Viêm gân gấp chân trước trái', DATEADD(day, -12, CAST(GETDATE() AS DATE)), NULL);

                UPDATE [HorseHealthStatus]
                SET [TrangThai] = N'Đang điều trị', [NgayCapNhat] = GETDATE()
                WHERE [HorseID] = 3;
            END;
        END;
        """,
        """
        UPDATE [MedicalRecord]
        SET [ViTriChanThuong] = N'Không có chấn thương'
        WHERE [ViTriChanThuong] IS NULL;
        """,
        """
        IF NOT EXISTS (SELECT 1 FROM [VaccinationSchedule])
        BEGIN
            INSERT INTO [VaccinationSchedule] ([HorseID], [Loai], [NgayHen], [TrangThai]) VALUES
            (1, N'Cúm ngựa', DATEADD(day, -40, CAST(GETDATE() AS DATE)), N'Đã tiêm'),
            (1, N'Uốn ván', DATEADD(day, 4, CAST(GETDATE() AS DATE)), N'Chờ tiêm'),
            (2, N'Uốn ván', DATEADD(day, -60, CAST(GETDATE() AS DATE)), N'Đã tiêm'),
            (2, N'Cúm ngựa', DATEADD(day, -15, CAST(GETDATE() AS DATE)), N'Đã tiêm'),
            (2, N'Viêm não Nhật Bản', DATEADD(day, -75, CAST(GETDATE() AS DATE)), N'Đã tiêm'),
            (3, N'Cúm ngựa', DATEADD(day, -90, CAST(GETDATE() AS DATE)), N'Đã tiêm'),
            (3, N'Uốn ván', DATEADD(day, -130, CAST(GETDATE() AS DATE)), N'Đã tiêm'),
            (4, N'Uốn ván', DATEADD(day, -30, CAST(GETDATE() AS DATE)), N'Đã tiêm'),
            (4, N'Cúm ngựa', DATEADD(day, -10, CAST(GETDATE() AS DATE)), N'Đã tiêm'),
            (4, N'Viêm não Nhật Bản', DATEADD(day, 6, CAST(GETDATE() AS DATE)), N'Chờ tiêm'),
            (5, N'Cúm ngựa', DATEADD(day, -50, CAST(GETDATE() AS DATE)), N'Đã tiêm'),
            (5, N'Uốn ván', DATEADD(day, -95, CAST(GETDATE() AS DATE)), N'Đã tiêm'),
            (6, N'Uốn ván', DATEADD(day, -100, CAST(GETDATE() AS DATE)), N'Đã tiêm'),
            (6, N'Cúm ngựa', DATEADD(day, -3, CAST(GETDATE() AS DATE)), N'Chờ tiêm'),
            (6, N'Viêm não Nhật Bản', DATEADD(day, -160, CAST(GETDATE() AS DATE)), N'Đã tiêm'),
            (2, N'Tẩy giun', DATEADD(day, 45, CAST(GETDATE() AS DATE)), N'Chờ tiêm'),
            (1, N'Dại', DATEADD(day, -70, CAST(GETDATE() AS DATE)), N'Đã tiêm'),
            (2, N'Dại', DATEADD(day, -85, CAST(GETDATE() AS DATE)), N'Đã tiêm'),
            (4, N'Dại', DATEADD(day, -120, CAST(GETDATE() AS DATE)), N'Đã tiêm'),
            (6, N'Dại', DATEADD(day, -65, CAST(GETDATE() AS DATE)), N'Đã tiêm'),
            (3, N'Tẩy giun', DATEADD(day, -25, CAST(GETDATE() AS DATE)), N'Đã tiêm'),
            (5, N'Tẩy giun', DATEADD(day, -18, CAST(GETDATE() AS DATE)), N'Đã tiêm'),
            (5, N'Viêm não Nhật Bản', DATEADD(day, -5, CAST(GETDATE() AS DATE)), N'Chờ tiêm');
        END;
        """
    ];

    public static void Apply(ApplicationDbContext db)
    {
        foreach (var statement in Statements)
        {
            db.Database.ExecuteSqlRaw(statement);
        }
    }
}
