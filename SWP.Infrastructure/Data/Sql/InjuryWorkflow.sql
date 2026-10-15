-- Nâng cấp bổ sung, không chèn dữ liệu mẫu và không xóa dữ liệu cũ.
-- API chạy toàn bộ script trong transaction. Ngày giờ lưu UTC.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.InjuryCase', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InjuryCase (
        InjuryCaseId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        HorseId INT NOT NULL,
        CreatedByUserId INT NOT NULL,
        Title NVARCHAR(150) NOT NULL,
        BodyRegion NVARCHAR(100) NOT NULL,
        Side NVARCHAR(20) NOT NULL,
        Severity NVARCHAR(20) NOT NULL,
        Status NVARCHAR(20) NOT NULL,
        OwnerSummary NVARCHAR(1000) NOT NULL,
        TrainingGuidance NVARCHAR(1000) NOT NULL,
        ReviewDueAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL,
        UpdatedAt DATETIME2 NOT NULL,
        ClosedAt DATETIME2 NULL,
        Version INT NOT NULL CONSTRAINT DF_InjuryCase_Version DEFAULT 1,
        CONSTRAINT FK_InjuryCase_Horse_HorseId FOREIGN KEY (HorseId) REFERENCES dbo.Horse(HorseID),
        CONSTRAINT FK_InjuryCase_Users_CreatedByUserId FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(UserID)
    );
END;
IF OBJECT_ID(N'dbo.InjuryCaseEvent', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InjuryCaseEvent (
        InjuryCaseEventId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        InjuryCaseId INT NOT NULL,
        ActorUserId INT NOT NULL,
        Kind NVARCHAR(40) NOT NULL,
        DetailsJson NVARCHAR(MAX) NOT NULL,
        MedicalRecordId INT NULL,
        CreatedAt DATETIME2 NOT NULL,
        CONSTRAINT FK_InjuryCaseEvent_InjuryCase_InjuryCaseId FOREIGN KEY (InjuryCaseId) REFERENCES dbo.InjuryCase(InjuryCaseId),
        CONSTRAINT FK_InjuryCaseEvent_Users_ActorUserId FOREIGN KEY (ActorUserId) REFERENCES dbo.Users(UserID),
        CONSTRAINT FK_InjuryCaseEvent_MedicalRecord_MedicalRecordId FOREIGN KEY (MedicalRecordId) REFERENCES dbo.MedicalRecord(MedicalRecordID)
    );
END;
GO
-- CareTask được dùng chung. Các việc cũ có InjuryCaseId NULL và giữ nguyên nghiệp vụ cũ.
IF COL_LENGTH('dbo.CareTask', 'InjuryCaseId') IS NULL
    ALTER TABLE dbo.CareTask ADD InjuryCaseId INT NULL;
IF COL_LENGTH('dbo.CareTask', 'AssignedByUserId') IS NULL
    ALTER TABLE dbo.CareTask ADD AssignedByUserId INT NULL;
IF COL_LENGTH('dbo.CareTask', 'Version') IS NULL
    ALTER TABLE dbo.CareTask ADD Version INT NOT NULL CONSTRAINT DF_CareTask_Version DEFAULT 1 WITH VALUES;
IF COL_LENGTH('dbo.TrainingLock', 'InjuryCaseId') IS NULL
    ALTER TABLE dbo.TrainingLock ADD InjuryCaseId INT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_CareTask_InjuryCase_InjuryCaseId')
    ALTER TABLE dbo.CareTask ADD CONSTRAINT FK_CareTask_InjuryCase_InjuryCaseId
        FOREIGN KEY (InjuryCaseId) REFERENCES dbo.InjuryCase(InjuryCaseId);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_CareTask_Users_AssignedByUserId')
    ALTER TABLE dbo.CareTask ADD CONSTRAINT FK_CareTask_Users_AssignedByUserId
        FOREIGN KEY (AssignedByUserId) REFERENCES dbo.Users(UserID);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_TrainingLock_InjuryCase_InjuryCaseId')
    ALTER TABLE dbo.TrainingLock ADD CONSTRAINT FK_TrainingLock_InjuryCase_InjuryCaseId
        FOREIGN KEY (InjuryCaseId) REFERENCES dbo.InjuryCase(InjuryCaseId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.InjuryCase') AND name = N'IX_InjuryCase_HorseId_Status')
    CREATE INDEX IX_InjuryCase_HorseId_Status ON dbo.InjuryCase(HorseId, Status);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.InjuryCaseEvent') AND name = N'IX_InjuryCaseEvent_InjuryCaseId_InjuryCaseEventId')
    CREATE INDEX IX_InjuryCaseEvent_InjuryCaseId_InjuryCaseEventId ON dbo.InjuryCaseEvent(InjuryCaseId, InjuryCaseEventId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.InjuryCaseEvent') AND name = N'IX_InjuryCaseEvent_InjuryCaseId_MedicalRecordId')
    CREATE UNIQUE INDEX IX_InjuryCaseEvent_InjuryCaseId_MedicalRecordId ON dbo.InjuryCaseEvent(InjuryCaseId, MedicalRecordId) WHERE MedicalRecordId IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.CareTask') AND name = N'IX_CareTask_InjuryCaseId_Status_ScheduledAt')
    CREATE INDEX IX_CareTask_InjuryCaseId_Status_ScheduledAt ON dbo.CareTask(InjuryCaseId, Status, ScheduledAt);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.TrainingLock') AND name = N'IX_TrainingLock_InjuryCaseId')
    CREATE INDEX IX_TrainingLock_InjuryCaseId ON dbo.TrainingLock(InjuryCaseId);
GO
-- Chú thích tiếng Việt hiển thị trong Extended Properties của SSMS.
DECLARE @Descriptions TABLE (TableName SYSNAME, ColumnName SYSNAME NULL, Description NVARCHAR(1000));
INSERT INTO @Descriptions VALUES
(N'InjuryCase', NULL, N'Hồ sơ một đợt chấn thương. Một ngựa có thể có nhiều hồ sơ song song; đóng hồ sơ không tự gỡ khóa tập.'),
(N'InjuryCaseEvent', NULL, N'Lịch sử chỉ ghi thêm qua API, gồm người thực hiện, thời gian UTC và bản chụp bệnh án. Chỉ bác sĩ đọc nội dung.'),
(N'InjuryCase', N'Status', N'Open: mới mở; Treating: đang điều trị; Monitoring: theo dõi; Closed: đã kết thúc. Chỉ bác sĩ chuyển trạng thái.'),
(N'InjuryCase', N'OwnerSummary', N'Tóm tắt bác sĩ soạn để chủ ngựa đọc; tách biệt ghi chú lâm sàng.'),
(N'InjuryCase', N'TrainingGuidance', N'Hướng dẫn vận động cho HLV. Chỉ dẫn văn bản không thay thế lệnh khóa tập do backend kiểm tra.'),
(N'InjuryCase', N'ReviewDueAt', N'Hạn tái khám theo UTC; quá hạn chỉ cảnh báo, không tự mở khóa hoặc đóng hồ sơ.'),
(N'InjuryCase', N'Version', N'Phiên bản chống ghi đè khi nhiều người thao tác; request sửa phải gửi phiên bản mới nhất.'),
(N'CareTask', N'InjuryCaseId', N'Hồ sơ chấn thương liên quan; NULL đối với các việc chăm sóc thông thường trước đây.'),
(N'CareTask', N'AssignedByUserId', N'Bác sĩ giao công việc chăm sóc; lấy từ tài khoản đăng nhập.'),
(N'CareTask', N'Version', N'Phiên bản công việc để chống cập nhật đè; dữ liệu cũ được khởi tạo bằng 1.'),
(N'TrainingLock', N'InjuryCaseId', N'Hồ sơ phát sinh khóa tập; NULL đối với khóa cũ. Một hồ sơ đóng không mở khóa của hồ sơ khác.');
DECLARE @Table SYSNAME, @Column SYSNAME, @Description NVARCHAR(1000);
DECLARE descriptions CURSOR LOCAL FAST_FORWARD FOR SELECT TableName, ColumnName, Description FROM @Descriptions;
OPEN descriptions;
FETCH NEXT FROM descriptions INTO @Table, @Column, @Description;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE name=N'MS_Description'
        AND major_id=OBJECT_ID(N'dbo.' + @Table)
        AND minor_id=CASE WHEN @Column IS NULL THEN 0 ELSE COLUMNPROPERTY(OBJECT_ID(N'dbo.' + @Table), @Column, 'ColumnId') END)
    BEGIN
        IF @Column IS NULL
            EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=@Description,
                @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=@Table;
        ELSE
            EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=@Description,
                @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=@Table,
                @level2type=N'COLUMN', @level2name=@Column;
    END;
    FETCH NEXT FROM descriptions INTO @Table, @Column, @Description;
END;
CLOSE descriptions;
DEALLOCATE descriptions;
