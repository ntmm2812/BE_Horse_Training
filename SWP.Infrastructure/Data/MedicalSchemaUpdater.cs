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
        -- Chỉ bổ sung cấu trúc. Không tạo bệnh án, khóa tập hay lịch tiêm mẫu khi chạy ứng dụng.
        -- Cho phép NULL để giữ nguyên các bản ghi lịch sử chưa có thông tin mở khóa.
        IF COL_LENGTH('TrainingLock', 'ReleasedByUserId') IS NULL
            ALTER TABLE [TrainingLock] ADD [ReleasedByUserId] INT NULL;
        IF COL_LENGTH('TrainingLock', 'ReleaseReason') IS NULL
            ALTER TABLE [TrainingLock] ADD [ReleaseReason] NVARCHAR(500) NULL;
        IF COL_LENGTH('TrainingLock', 'ReexamRecordId') IS NULL
            ALTER TABLE [TrainingLock] ADD [ReexamRecordId] INT NULL;
        """,
        """
        -- Chú thích có thể xem tại Column Properties / Extended Properties trong SSMS.
        IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE major_id = OBJECT_ID(N'dbo.TrainingLock')
            AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.TrainingLock'), 'ReleasedByUserId', 'ColumnId') AND name = N'MS_Description')
            EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Bác sĩ mở khóa, lấy từ tài khoản đăng nhập; NULL đối với dữ liệu lịch sử chưa ghi nhận.',
                @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'TrainingLock', @level2type=N'COLUMN', @level2name=N'ReleasedByUserId';
        IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE major_id = OBJECT_ID(N'dbo.TrainingLock')
            AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.TrainingLock'), 'ReleaseReason', 'ColumnId') AND name = N'MS_Description')
            EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Lý do mở khóa sau tái khám, tối đa 500 ký tự; bắt buộc cho thao tác mở khóa mới.',
                @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'TrainingLock', @level2type=N'COLUMN', @level2name=N'ReleaseReason';
        IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE major_id = OBJECT_ID(N'dbo.TrainingLock')
            AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.TrainingLock'), 'ReexamRecordId', 'ColumnId') AND name = N'MS_Description')
            EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Bệnh án tái khám hoàn tất của đúng ngựa dùng làm căn cứ mở khóa.',
                @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'TrainingLock', @level2type=N'COLUMN', @level2name=N'ReexamRecordId';
        """,
        """
        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_TrainingLock_ReleasedBy')
            ALTER TABLE [TrainingLock] ADD CONSTRAINT [FK_TrainingLock_ReleasedBy]
                FOREIGN KEY ([ReleasedByUserId]) REFERENCES [Users]([UserID]);
        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_TrainingLock_ReexamRecord')
            ALTER TABLE [TrainingLock] ADD CONSTRAINT [FK_TrainingLock_ReexamRecord]
                FOREIGN KEY ([ReexamRecordId]) REFERENCES [MedicalRecord]([MedicalRecordID]);
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'TrainingLock') AND name = N'IX_TrainingLock_ActiveHorse')
            CREATE INDEX [IX_TrainingLock_ActiveHorse] ON [TrainingLock] ([HorseID]) WHERE [NgayGo] IS NULL;
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
