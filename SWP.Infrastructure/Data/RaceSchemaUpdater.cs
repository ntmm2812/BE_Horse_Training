using Microsoft.EntityFrameworkCore;

namespace SWP.Infrastructure.Persistence;

/// <summary>
/// Một ngựa chỉ có một lượt đăng ký cho mỗi giải; giải đua có thêm 3 cột giải thưởng theo hạng (nullable, giải cũ để NULL).
/// </summary>
public static class RaceSchemaUpdater
{
    public static void Apply(ApplicationDbContext db) => db.Database.ExecuteSqlRaw(Sql);

    public const string Sql = """
        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE name = N'UX_RaceRegistration_Race_Horse'
              AND object_id = OBJECT_ID(N'RaceRegistration'))
            CREATE UNIQUE INDEX [UX_RaceRegistration_Race_Horse]
                ON [RaceRegistration]([RaceID], [HorseID]);
        IF COL_LENGTH('Race', 'PrizeFirst') IS NULL
            ALTER TABLE [Race] ADD [PrizeFirst] DECIMAL(18, 2) NULL;
        IF COL_LENGTH('Race', 'PrizeSecond') IS NULL
            ALTER TABLE [Race] ADD [PrizeSecond] DECIMAL(18, 2) NULL;
        IF COL_LENGTH('Race', 'PrizeThird') IS NULL
            ALTER TABLE [Race] ADD [PrizeThird] DECIMAL(18, 2) NULL;
        """;
}
