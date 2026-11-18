using Microsoft.EntityFrameworkCore;

namespace SWP.Infrastructure.Persistence;

/// <summary> Nhật ký thao tác: tạo bảng nếu thiếu, hỗ trợ tiếng Việt, thêm tên đối tượng và chỉ mục truy vấn. </summary>
public static class AuditLogSchemaUpdater
{
    private static readonly string[] Statements =
    [
        """
        IF OBJECT_ID(N'AuditLog', N'U') IS NULL
            CREATE TABLE [AuditLog] (
                [LogID] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                [UserID] INT NULL,
                [Action] NVARCHAR(300) NOT NULL,
                [TableName] VARCHAR(50) NOT NULL,
                [RecordID] INT NULL,
                [OldValue] NVARCHAR(MAX) NULL,
                [NewValue] NVARCHAR(MAX) NULL,
                [IPAddress] VARCHAR(45) NULL,
                [CreatedAt] DATETIME NOT NULL,
                CONSTRAINT [FK_AuditLog_Users] FOREIGN KEY ([UserID]) REFERENCES [Users]([UserID])
            );
        """,
        """
        IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'AuditLog') AND name = 'Action'
                   AND (system_type_id <> 231 OR max_length <> 600))
            ALTER TABLE [AuditLog] ALTER COLUMN [Action] NVARCHAR(300) NOT NULL;
        """,
        """
        IF COL_LENGTH('AuditLog', 'TargetLabel') IS NULL
            ALTER TABLE [AuditLog] ADD [TargetLabel] NVARCHAR(200) NULL;
        """,
        """
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AuditLog_CreatedAt' AND object_id = OBJECT_ID(N'AuditLog'))
            CREATE INDEX [IX_AuditLog_CreatedAt] ON [AuditLog] ([CreatedAt] DESC);
        """,
        """
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AuditLog_UserID_CreatedAt' AND object_id = OBJECT_ID(N'AuditLog'))
            CREATE INDEX [IX_AuditLog_UserID_CreatedAt] ON [AuditLog] ([UserID], [CreatedAt] DESC);
        """,
    ];

    public static void Apply(ApplicationDbContext db)
    {
        foreach (var sql in Statements)
        {
            db.Database.ExecuteSqlRaw(sql);
        }
    }
}
