using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SWP.API.Realtime;
using SWP.Infrastructure.Persistence;

namespace SWP.API.Controllers;

[ApiController]
[Route("api/shared-state")]
public class SharedStateController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IHubContext<HorseActivityHub> _hub;

    public SharedStateController(ApplicationDbContext db, IHubContext<HorseActivityHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    public sealed record SharedStateUpdateDto(string? Value);

    private async Task EnsureTableAsync(CancellationToken cancellationToken)
    {
        if (!_db.Database.IsSqlServer())
        {
            return;
        }

        await _db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'SharedClientState', N'U') IS NULL
            BEGIN
                CREATE TABLE [SharedClientState] (
                    [StateKey] NVARCHAR(128) NOT NULL PRIMARY KEY,
                    [JsonValue] NVARCHAR(MAX) NOT NULL,
                    [UpdatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
                );
            END;
            """, cancellationToken);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        await EnsureTableAsync(cancellationToken);
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!_db.Database.IsSqlServer())
        {
            return Ok(result);
        }

        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
        {
            await conn.OpenAsync(cancellationToken);
        }

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT [StateKey], [JsonValue] FROM [SharedClientState];";
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result[reader.GetString(0)] = reader.GetString(1);
        }

        return Ok(result);
    }

    [HttpPut("{key}")]
    public async Task<IActionResult> Upsert(
        string key,
        [FromBody] SharedStateUpdateDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 128)
        {
            return BadRequest(new { message = "State key không hợp lệ." });
        }

        await EnsureTableAsync(cancellationToken);
        if (_db.Database.IsSqlServer())
        {
            if (request.Value is null)
            {
                await _db.Database.ExecuteSqlInterpolatedAsync(
                    $"DELETE FROM [SharedClientState] WHERE [StateKey] = {key};",
                    cancellationToken);
            }
            else
            {
                await _db.Database.ExecuteSqlInterpolatedAsync($"""
                    MERGE [SharedClientState] AS target
                    USING (SELECT {key} AS [StateKey], {request.Value} AS [JsonValue]) AS source
                    ON target.[StateKey] = source.[StateKey]
                    WHEN MATCHED THEN
                        UPDATE SET [JsonValue] = source.[JsonValue], [UpdatedAt] = SYSUTCDATETIME()
                    WHEN NOT MATCHED THEN
                        INSERT ([StateKey], [JsonValue], [UpdatedAt])
                        VALUES (source.[StateKey], source.[JsonValue], SYSUTCDATETIME());
                    """, cancellationToken);
            }
        }

        await _hub.Clients.All.SendAsync("sharedStateChanged", new { key, value = request.Value }, cancellationToken);
        return Ok(new { key, updated = true });
    }

    [HttpDelete("{key}")]
    public async Task<IActionResult> Delete(string key, CancellationToken cancellationToken)
    {
        await EnsureTableAsync(cancellationToken);
        if (_db.Database.IsSqlServer())
        {
            await _db.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM [SharedClientState] WHERE [StateKey] = {key};",
                cancellationToken);
        }

        await _hub.Clients.All.SendAsync("sharedStateChanged", new { key, value = (string?)null }, cancellationToken);
        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> ClearAll(CancellationToken cancellationToken)
    {
        await EnsureTableAsync(cancellationToken);
        if (_db.Database.IsSqlServer())
        {
            await _db.Database.ExecuteSqlRawAsync("DELETE FROM [SharedClientState];", cancellationToken);
        }

        await _hub.Clients.All.SendAsync("sharedStateCleared", new { cleared = true }, cancellationToken);
        return NoContent();
    }
}

