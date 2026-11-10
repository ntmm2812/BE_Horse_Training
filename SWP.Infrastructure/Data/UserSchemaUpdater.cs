using Microsoft.EntityFrameworkCore;

namespace SWP.Infrastructure.Persistence;

/// <summary> Bổ sung cột MustChangePassword trên Users cho SQL Server có sẵn. </summary>
public static class UserSchemaUpdater
{
    private static readonly string[] Statements =
    [
        """
        IF COL_LENGTH('Users', 'MustChangePassword') IS NULL
            ALTER TABLE [Users] ADD [MustChangePassword] BIT NOT NULL
                CONSTRAINT [DF_Users_MustChangePassword] DEFAULT (0);
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
