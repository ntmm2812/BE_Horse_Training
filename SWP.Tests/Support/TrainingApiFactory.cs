using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SWP.Domain.Models.Entity;
using SWP.Infrastructure.Persistence;
using SWP.Infrastructure.Security.Interface;

namespace SWP.Tests.Support;

/// <summary> Chạy API thật (Program.cs) trên SQLite in-memory với đồng hồ cố định. </summary>
public sealed class TrainingApiFactory : WebApplicationFactory<SWP.API.Program>
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly SqliteConnection _connection = new("DataSource=:memory:;Foreign Keys=True");
    private readonly object _seedLock = new();
    private SeededIds? _ids;

    public FixedTimeProvider Clock { get; } = new(TestData.Now);

    public SeededIds Ids
    {
        get
        {
            EnsureSeeded();
            return _ids!;
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();
        builder.UseEnvironment("Development");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));

            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }

    public int AddHorse(string name, string? healthStatus = "Đủ điều kiện") =>
        WithDb(db => TestData.AddHorse(db, Ids.OwnerId, name, healthStatus: healthStatus));

    public T WithDb<T>(Func<ApplicationDbContext, T> action)
    {
        using var scope = Services.CreateScope();
        return action(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    }

    public HttpClient Anonymous() => CreateClient();

    public HttpClient ClientAs(int userId, string roleName)
    {
        var token = Services.GetRequiredService<IJwtTokenGenerator>().GenerateToken(new User
        {
            UserId = userId,
            FullName = $"User {userId}",
            Email = $"user{userId}@test.vn",
            Role = new Role { RoleName = roleName },
        });

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public HttpClient Trainer() => ClientAs(Ids.TrainerId, "Head Trainer");

    public HttpClient Groom() => ClientAs(Ids.GroomId, "Groom");

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }

    private void EnsureSeeded()
    {
        lock (_seedLock)
        {
            if (_ids is not null)
            {
                return;
            }

            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
            _ids = TestData.SeedBasics(db);
        }
    }
}
