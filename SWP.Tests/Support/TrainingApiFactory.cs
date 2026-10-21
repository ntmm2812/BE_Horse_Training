using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using SWP.Domain.Models.Entity;
using SWP.Infrastructure.Persistence;
using SWP.Infrastructure.Security.Interface;

namespace SWP.Tests.Support;

/// <summary> Chạy API thật (Program.cs) trên SQLite in-memory với đồng hồ cố định. </summary>
public class TrainingApiFactory : WebApplicationFactory<SWP.API.Program>
{
    protected virtual bool UseOriginalSchema => false;
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
        // Test không phụ thuộc quyền ghi Windows Event Log của máy chạy.
        builder.ConfigureLogging(logging => logging.ClearProviders().AddConsole());
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));

            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
            services.AddTransient<IStartupFilter, TestBufferStartupFilter>();
        });
    }

    private sealed class TestBufferStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                app.Use(async (context, nextMiddleware) =>
                {
                    var originalBody = context.Response.Body;
                    using var memStream = new MemoryStream();
                    context.Response.Body = memStream;
                    try
                    {
                        await nextMiddleware();
                    }
                    finally
                    {
                        memStream.Position = 0;
                        await memStream.CopyToAsync(originalBody);
                        context.Response.Body = originalBody;
                    }
                });
                next(app);
            };
        }
    }

    public int AddHorse(string name, string? healthStatus = "Đủ điều kiện") =>
        WithDb(db => TestData.AddHorse(db, Ids.OwnerId, name, healthStatus: healthStatus));

    public T WithDb<T>(Func<ApplicationDbContext, T> action)
    {
        using var scope = Services.CreateScope();
        return action(ResolveDb(scope.ServiceProvider));
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
            var db = ResolveDb(scope.ServiceProvider);
            db.Database.EnsureCreated();
            _ids = TestData.SeedBasics(db);
        }
    }

    private ApplicationDbContext ResolveDb(IServiceProvider services) =>
        services.GetRequiredService<ApplicationDbContext>();
}

public sealed class OriginalCareApiFactory : TrainingApiFactory
{
    protected override bool UseOriginalSchema => true;
}
