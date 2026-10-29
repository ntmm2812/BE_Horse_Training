using Microsoft.EntityFrameworkCore;

namespace SWP.Infrastructure.Persistence;

/// <summary> Một ngựa chỉ có một lượt đăng ký cho mỗi giải. </summary>
public static class RaceSchemaUpdater
{
    public static void Apply(ApplicationDbContext db)
    {
        db.Database.ExecuteSqlRaw(
            """
            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = N'UX_RaceRegistration_Race_Horse'
                  AND object_id = OBJECT_ID(N'RaceRegistration'))
                CREATE UNIQUE INDEX [UX_RaceRegistration_Race_Horse]
                    ON [RaceRegistration]([RaceID], [HorseID]);
            """);
    }
}
