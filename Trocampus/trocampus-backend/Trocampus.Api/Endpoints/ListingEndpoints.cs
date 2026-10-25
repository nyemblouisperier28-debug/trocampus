using Npgsql;
using Trocampus.Api.Contracts;
using Trocampus.Api.Services;

namespace Trocampus.Api.Endpoints;

public static class ListingEndpoints
{
    public static void MapListingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api").WithTags("Listings");
        group.MapGet("/listings", async (string? q, long? categoryId, long? zoneId, int? radiusM, string? type,
            int? take, int? skip, ListingService listings, CancellationToken token) =>
        {
            if (type is not null && type is not ("sale" or "exchange"))
                return Results.BadRequest(new { message = "Le type doit être sale ou exchange." });
            if (radiusM is not null && radiusM is < 100 or > 50000)
                return Results.BadRequest(new { message = "Le rayon doit être compris entre 100 m et 50 km." });
            var results = await listings.GetListingsAsync(q, categoryId, zoneId, radiusM, type,
                Math.Clamp(take ?? 20, 1, 100), Math.Max(skip ?? 0, 0), token);
            return Results.Ok(results);
        });
        group.MapGet("/categories", async (ListingService listings, CancellationToken token) =>
            Results.Ok(await listings.GetCategoriesAsync(token)));
        group.MapGet("/zones", async (NpgsqlDataSource dataSource, CancellationToken token) =>
        {
            await using var connection = await dataSource.OpenConnectionAsync(token);
            await using var command = new NpgsqlCommand("SELECT z.id, z.name, c.name FROM zones z JOIN campuses c ON c.id = z.campus_id ORDER BY c.name, z.name", connection);
            var zones = new List<ZoneResponse>();
            await using var reader = await command.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token)) zones.Add(new ZoneResponse(reader.GetInt64(0), reader.GetString(1), reader.GetString(2)));
            return Results.Ok(zones);
        });
        group.MapPost("/listings", async (CreateListingRequest request, HttpContext context,
            AuthService auth, ListingService listings, CancellationToken token) =>
        {
            var userId = await auth.GetUserIdAsync(AuthEndpoints.ReadBearerToken(context), token);
            if (userId is null) return Results.Unauthorized();
            try
            {
                var listing = await listings.CreateListingAsync(userId.Value, request, token);
                return listing is null
                    ? Results.BadRequest(new { message = "Une annonce de vente doit indiquer un prix en euros ou en points." })
                    : Results.Created($"/api/listings/{listing.Id}", listing);
            }
            catch (PostgresException exception) when (exception.SqlState == Npgsql.PostgresErrorCodes.ForeignKeyViolation)
            {
                return Results.BadRequest(new { message = "La catégorie ou la zone indiquée n'existe pas." });
            }
        });
    }
}