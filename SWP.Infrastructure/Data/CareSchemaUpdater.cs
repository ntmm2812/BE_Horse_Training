using Microsoft.EntityFrameworkCore;

namespace SWP.Infrastructure.Persistence;

/// <summary>
/// Bổ sung báo cáo sự cố và cho phép loại vật tư tiếng Việt trên SQL Server có sẵn.
/// </summary>
public static class CareSchemaUpdater
{
    private static readonly string[] Statements =
    [
        """
        IF OBJECT_ID(N'IncidentReport', N'U') IS NULL
        BEGIN
            CREATE TABLE [IncidentReport] (
                [ReportID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                [HorseID] INT NOT NULL,
                [GroomID] INT NOT NULL,
                [LoaiSuCo] NVARCHAR(50) NOT NULL,
                [MoTa] NVARCHAR(300) NULL,
                [HinhAnh] NVARCHAR(MAX) NULL,
                [Ngay] DATE NOT NULL,
                [CreatedAt] DATETIME NOT NULL,
                CONSTRAINT [FK_IncidentReport_Horse] FOREIGN KEY ([HorseID]) REFERENCES [Horse]([HorseID]) ON DELETE CASCADE,
                CONSTRAINT [FK_IncidentReport_Groom] FOREIGN KEY ([GroomID]) REFERENCES [Users]([UserID])
            );
        END;
        """,
        """
        IF COL_LENGTH('Supply', 'Category') IS NOT NULL
           AND EXISTS (
                SELECT 1
                FROM sys.columns c
                INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
                WHERE c.object_id = OBJECT_ID(N'Supply')
                  AND c.name = N'Category'
                  AND t.name = N'varchar')
            ALTER TABLE [Supply] ALTER COLUMN [Category] NVARCHAR(50) NULL;
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
