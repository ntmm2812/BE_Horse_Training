using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SWP.Application.Services.Implement;
using SWP.Infrastructure.Persistence;
using SWP.Infrastructure.Repositories.Implement;

namespace SWP.Tests.Support;

/// <summary>
/// SQLite in-memory + repository/service thật. Mỗi lần lấy service là một DbContext mới,
/// giống phạm vi một HTTP request.
/// </summary>
public sealed class TrainingTestContext : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly List<ApplicationDbContext> _contexts = new();

    public TrainingTestContext()
    {
        _connection = new SqliteConnection("DataSource=:memory:;Foreign Keys=True");
        _connection.Open();
        Clock = new FixedTimeProvider(TestData.Now);

        using var db = NewContext(track: false);
        db.Database.EnsureCreated();
        Ids = TestData.SeedBasics(db);
    }

    public FixedTimeProvider Clock { get; }

    public SeededIds Ids { get; }

    public ApplicationDbContext NewContext(bool track = true)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;
        var context = new ApplicationDbContext(options);
        if (track)
        {
            _contexts.Add(context);
        }

        return context;
    }

    /// <summary> Thao tác trực tiếp DB (seed/kiểm tra) trên một context riêng. </summary>
    public T Db<T>(Func<ApplicationDbContext, T> action)
    {
        using var db = NewContext(track: false);
        return action(db);
    }

    public void Db(Action<ApplicationDbContext> action)
    {
        using var db = NewContext(track: false);
        action(db);
    }

    public int AddHorse(string name, int ageYears = 5, string? healthStatus = "Đủ điều kiện", string? lockReason = null) =>
        Db(db => TestData.AddHorse(db, Ids.OwnerId, name, ageYears, healthStatus, lockReason));

    public HorseFitnessService FitnessService() => FitnessService(NewContext());

    public TrainingPlanService PlanService()
    {
        var db = NewContext();
        return new TrainingPlanService(
            new TrainingPlanRepository(db),
            new TrainingSessionRepository(db),
            new HorseRepository(db),
            new UserRepository(db),
            FitnessService(db),
            Clock);
    }

    public TrainingSessionService SessionService()
    {
        var db = NewContext();
        return new TrainingSessionService(
            new TrainingSessionRepository(db),
            new TrainingPlanRepository(db),
            new HorseRepository(db),
            new UserRepository(db),
            FitnessService(db),
            Clock);
    }

    public MedicalService MedicalService()
    {
        var db = NewContext();
        return new MedicalService(
            new MedicalRepository(db),
            new HorseRepository(db),
            new HorseHealthStatusRepository(db));
    }

    /// <summary> Tạo giáo án 3 giai đoạn qua service và áp dụng (Active). </summary>
    public async Task<int> CreateActivePlanAsync(int horseId)
    {
        var created = await PlanService().CreateAsync(TestData.ThreePhasePlan(horseId), Ids.TrainerId);
        Assert.True(created.Success, created.ErrorMessage);
        var activated = await PlanService().ActivateAsync(created.Data!.PlanId);
        Assert.True(activated.Success, activated.ErrorMessage);
        return created.Data.PlanId;
    }

    public void Dispose()
    {
        foreach (var context in _contexts)
        {
            context.Dispose();
        }

        _connection.Dispose();
    }

    private HorseFitnessService FitnessService(ApplicationDbContext db) =>
        new(new HorseRepository(db), new HorseHealthStatusRepository(db), new TrainingSessionRepository(db), Clock, new MedicalRepository(db));
}
