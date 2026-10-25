using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Npgsql;
using Trocampus.Api.Contracts;

namespace Trocampus.Api.Services;

public sealed class ListingService(NpgsqlDataSource dataSource, IDistributedCache cache)
{
    public async Task<IReadOnlyList<ListingResponse>> GetListingsAsync(string? query, long? categoryId,
        long? zoneId, int? radiusMeters, string? listingType, int take, int skip, CancellationToken cancellationToken)
    {
        var normalizedQuery = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
        var cacheKeySource = $"{normalizedQuery}|{categoryId}|{zoneId}|{radiusMeters}|{listingType}|{take}|{skip}";
        var cacheKey = "listings:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(cacheKeySource)));
        var cached = await cache.GetStringAsync(cacheKey, cancellationToken);
        if (cached is not null)
            return JsonSerializer.Deserialize<List<ListingResponse>>(cached) ?? [];

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand("""
            SELECT l.id, l.title, l.description, u.first_name,
                   c.name, ca.name, z.name, l.condition, l.listing_type,
                   l.price_cents, l.price_points, l.wanted_text, l.created_at
            FROM listings l
            JOIN users u ON u.id = l.owner_id
            JOIN categories ca ON ca.id = l.category_id
            JOIN zones z ON z.id = l.zone_id
            JOIN campuses c ON c.id = z.campus_id
            WHERE l.status = 'active'
              AND (@query::text IS NULL OR l.search_vector @@ websearch_to_tsquery('french', @query::text))
              AND (@category_id::bigint IS NULL OR l.category_id = @category_id::bigint)
              AND (@zone_id::bigint IS NULL OR @radius_m::integer IS NOT NULL OR l.zone_id = @zone_id::bigint)
              AND (@zone_id::bigint IS NULL OR @radius_m::integer IS NULL OR distance_m(
                  (SELECT latitude FROM zones WHERE id = @zone_id::bigint),
                  (SELECT longitude FROM zones WHERE id = @zone_id::bigint), z.latitude, z.longitude) <= @radius_m::integer)
              AND (@listing_type::text IS NULL OR l.listing_type = @listing_type::text)
            ORDER BY l.created_at DESC
            LIMIT @take OFFSET @skip
            """, connection);
        command.Parameters.AddWithValue("query", (object?)normalizedQuery ?? DBNull.Value);
        command.Parameters.AddWithValue("category_id", (object?)categoryId ?? DBNull.Value);
        command.Parameters.AddWithValue("zone_id", (object?)zoneId ?? DBNull.Value);
        command.Parameters.AddWithValue("radius_m", (object?)radiusMeters ?? DBNull.Value);
        command.Parameters.AddWithValue("listing_type", (object?)listingType ?? DBNull.Value);
        command.Parameters.AddWithValue("take", take);
        command.Parameters.AddWithValue("skip", skip);

        var listings = new List<ListingResponse>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            listings.Add(new ListingResponse(
                reader.GetInt64(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.GetString(3), reader.GetString(4), reader.GetString(5), reader.GetString(6),
                reader.IsDBNull(7) ? null : reader.GetString(7), reader.GetString(8),
                reader.IsDBNull(9) ? null : reader.GetInt32(9), reader.IsDBNull(10) ? null : reader.GetInt32(10),
                reader.IsDBNull(11) ? null : reader.GetString(11), reader.GetFieldValue<DateTimeOffset>(12)));
        }

        await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(listings), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(45)
        }, cancellationToken);
        return listings;
    }

    public async Task<ListingResponse?> CreateListingAsync(long ownerId, CreateListingRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ListingType == "sale" && request.PriceCents is null && request.PricePoints is null)
            return null;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand("""
            INSERT INTO listings (owner_id, category_id, zone_id, title, description, condition,
                                  listing_type, price_cents, price_points, wanted_text, status)
            VALUES (@owner_id, @category_id, @zone_id, @title, @description, @condition,
                    @listing_type, @price_cents, @price_points, @wanted_text, @status)
            RETURNING id
            """, connection);
        command.Parameters.AddWithValue("owner_id", ownerId);
        command.Parameters.AddWithValue("category_id", request.CategoryId);
        command.Parameters.AddWithValue("zone_id", request.ZoneId);
        command.Parameters.AddWithValue("title", request.Title.Trim());
        command.Parameters.AddWithValue("description", (object?)request.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("condition", (object?)request.Condition ?? DBNull.Value);
        command.Parameters.AddWithValue("listing_type", request.ListingType);
        command.Parameters.AddWithValue("price_cents", (object?)request.PriceCents ?? DBNull.Value);
        command.Parameters.AddWithValue("price_points", (object?)request.PricePoints ?? DBNull.Value);
        command.Parameters.AddWithValue("wanted_text", (object?)request.WantedText ?? DBNull.Value);
        command.Parameters.AddWithValue("status", request.Status);
        var listingId = (long)(await command.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("Could not create listing."));

        await using var fetch = new NpgsqlCommand("""
            SELECT l.id, l.title, l.description, u.first_name, c.name, ca.name, z.name,
                   l.condition, l.listing_type, l.price_cents, l.price_points, l.wanted_text, l.created_at
            FROM listings l JOIN users u ON u.id = l.owner_id JOIN categories ca ON ca.id = l.category_id
            JOIN zones z ON z.id = l.zone_id JOIN campuses c ON c.id = z.campus_id WHERE l.id = @id
            """, connection);
        fetch.Parameters.AddWithValue("id", listingId);
        await using var reader = await fetch.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new ListingResponse(reader.GetInt64(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2),
            reader.GetString(3), reader.GetString(4), reader.GetString(5), reader.GetString(6),
            reader.IsDBNull(7) ? null : reader.GetString(7), reader.GetString(8),
            reader.IsDBNull(9) ? null : reader.GetInt32(9), reader.IsDBNull(10) ? null : reader.GetInt32(10),
            reader.IsDBNull(11) ? null : reader.GetString(11), reader.GetFieldValue<DateTimeOffset>(12));
    }

    public async Task<IReadOnlyList<CategoryResponse>> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        const string cacheKey = "categories:all";
        var cached = await cache.GetStringAsync(cacheKey, cancellationToken);
        if (cached is not null) return JsonSerializer.Deserialize<List<CategoryResponse>>(cached) ?? [];

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand("SELECT id, name FROM categories ORDER BY name", connection);
        var categories = new List<CategoryResponse>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) categories.Add(new CategoryResponse(reader.GetInt64(0), reader.GetString(1)));
        await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(categories), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        }, cancellationToken);
        return categories;
    }
}