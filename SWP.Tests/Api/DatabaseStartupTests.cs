using System.Data.Common;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using SWP.Infrastructure.Persistence;

namespace SWP.Tests.Api;

public class DatabaseStartupTests
{
    [Fact]
    public async Task StartupDoesNotOpenOrModifySqlServerDatabase()
    {
        var guard = new NoDatabaseConnections();
        await using var app = new WebApplicationFactory<SWP.API.Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
                services.AddDbContext<ApplicationDbContext>(options => options
                    .UseSqlServer("Server=unused.invalid;Database=NeverConnect;Integrated Security=True;Connect Timeout=1")
                    .AddInterceptors(guard));
                services.RemoveAll<DbContextOptions<OriginalCareDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<OriginalCareDbContext>>();
                services.AddDbContext<OriginalCareDbContext>(options => options
                    .UseSqlServer("Server=unused.invalid;Database=NeverConnect;Integrated Security=True;Connect Timeout=1")
                    .AddInterceptors(guard));
            });
        });
        using var client = app.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/medical/records")).StatusCode);
        Assert.Equal(0, guard.Attempts);
    }

    private sealed class NoDatabaseConnections : DbConnectionInterceptor
    {
        public int Attempts { get; private set; }
        public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
        {
            Attempts++;
            throw new InvalidOperationException("Startup must not connect to the database.");
        }
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData,
            InterceptionResult result, CancellationToken cancellationToken = default)
        {
            Attempts++;
            throw new InvalidOperationException("Startup must not connect to the database.");
        }
    }
}
