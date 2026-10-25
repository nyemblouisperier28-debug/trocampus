using Microsoft.Extensions.Caching.Distributed;
using Npgsql;

namespace Trocampus.Api.Endpoints;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/health/live", () => Results.Ok(new { status = "ok" })).WithTags("Health");
        app.MapGet("/health/ready", async (NpgsqlDataSource dataSource, IDistributedCache cache,
            CancellationToken token) =>
        {
            await using var connection = await dataSource.OpenConnectionAsync(token);
            await using var command = new NpgsqlCommand("SELECT 1", connection);
            await command.ExecuteScalarAsync(token);
            await cache.GetStringAsync("health:probe", token);
            return Results.Ok(new { status = "ready", database = "ok", cache = "ok" });
        }).WithTags("Health");
    }
}