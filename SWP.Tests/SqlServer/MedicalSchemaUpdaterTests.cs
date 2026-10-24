using Microsoft.EntityFrameworkCore;
using SWP.Domain.Models.Entity;
using SWP.Infrastructure.Persistence;
using SWP.Tests.Support;

namespace SWP.Tests.SqlServer;

public class MedicalSchemaUpdaterTests
{
    [SqlServerFact]
    public void Upgrade_PreservesExistingData_WithoutSeeding()
    {
        var server = Environment.GetEnvironmentVariable(SqlServerFactAttribute.EnvironmentVariable)!.TrimEnd(';');
        var connectionString = $"{server};Database=SWP_MedicalSchema_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connectionString).Options;
        using var db = new ApplicationDbContext(options);
        try
        {
            db.Database.EnsureCreated();
            var ids = TestData.SeedBasics(db);
            var horseId = TestData.AddHorse(db, ids.OwnerId, "Existing horse");
            db.MedicalRecords.Add(new MedicalRecord { HorseId = horseId, CreateBy = ids.VetId,
                Diagnosis = "Bệnh án cũ chưa ghi vị trí", RecordDate = DateTime.Today, CreatedAt = DateTime.UtcNow, Status = "Completed" });
            db.SaveChanges();

            MedicalSchemaUpdater.Apply(db);
            MedicalSchemaUpdater.Apply(db);
            Assert.Empty(db.TrainingLocks.AsNoTracking());
            Assert.Empty(db.VaccinationSchedules.AsNoTracking());
            Assert.Null(db.MedicalRecords.AsNoTracking().Single().ViTriChanThuong);
            Assert.Equal("Đủ điều kiện", db.HorseHealthStatuses.AsNoTracking().Single().TrangThai);

            // Cũng hỗ trợ database gốc chưa có hai bảng y tế bổ sung.
            db.Database.ExecuteSqlRaw("DROP TABLE [TrainingLock]; DROP TABLE [VaccinationSchedule];");
            MedicalSchemaUpdater.Apply(db);
            Assert.Empty(db.TrainingLocks.AsNoTracking());
            Assert.Empty(db.VaccinationSchedules.AsNoTracking());
        }
        finally
        {
            db.Database.EnsureDeleted();
        }
    }
}
