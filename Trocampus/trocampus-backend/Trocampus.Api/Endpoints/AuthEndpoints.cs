using Npgsql;
using Trocampus.Api.Contracts;
using Trocampus.Api.Services;

namespace Trocampus.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Authentication");
        group.MapPost("/register", async (RegisterRequest request, AuthService auth, CancellationToken token) =>
        {
            var session = await auth.RegisterAsync(request, token);
            return session is null ? Results.Conflict(new { message = "Cette adresse e-mail est déjà utilisée." }) : Results.Ok(session);
        }).RequireRateLimiting("auth");
        group.MapPost("/login", async (LoginRequest request, AuthService auth, CancellationToken token) =>
        {
            var session = await auth.LoginAsync(request, token);
            return session is null ? Results.Unauthorized() : Results.Ok(session);
        }).RequireRateLimiting("auth");
        group.MapGet("/me", async (HttpContext context, AuthService auth, NpgsqlDataSource dataSource,
            CancellationToken token) =>
        {
            var userId = await auth.GetUserIdAsync(ReadBearerToken(context), token);
            if (userId is null)
                return Results.Unauthorized();

            await using var connection = await dataSource.OpenConnectionAsync(token);
            await using var command = new NpgsqlCommand("""
                SELECT u.id, u.first_name, u.last_name, u.email::text, u.role,
                       COALESCE(c.name, 'Campus non renseigné'), COALESCE(z.name, 'Zone non renseignée'),
                       COALESCE(points.balance, 0)::integer,
                       COALESCE(reviews.average_rating, 0)::double precision,
                       COALESCE(reviews.review_count, 0)::integer,
                       COALESCE(exchanges.completed, 0)::integer
                FROM users u
                LEFT JOIN campuses c ON c.id = u.campus_id
                LEFT JOIN zones z ON z.id = u.zone_id
                LEFT JOIN v_user_points points ON points.user_id = u.id
                LEFT JOIN LATERAL (
                    SELECT AVG(rating) AS average_rating, COUNT(*) AS review_count
                    FROM reviews WHERE reviewee_id = u.id AND status = 'visible'
                ) reviews ON true
                LEFT JOIN LATERAL (
                    SELECT COUNT(*) AS completed
                    FROM exchange_outcomes eo
                    JOIN appointments a ON a.id = eo.appointment_id
                    JOIN offers o ON o.id = a.offer_id
                    JOIN listings l ON l.id = o.listing_id
                    WHERE eo.outcome = 'success' AND (l.owner_id = u.id OR o.proposer_id = u.id)
                ) exchanges ON true
                WHERE u.id = @user_id AND u.status = 'active'
                """, connection);
            command.Parameters.AddWithValue("user_id", userId.Value);
            await using var reader = await command.ExecuteReaderAsync(token);
            if (!await reader.ReadAsync(token))
                return Results.Unauthorized();

            return Results.Ok(new CurrentUserResponse(
                reader.GetInt64(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.GetString(3), reader.GetString(4), reader.GetString(5), reader.GetString(6),
                reader.GetInt32(7), reader.GetDouble(8), reader.GetInt32(9), reader.GetInt32(10)));
        });
        group.MapPost("/logout", async (HttpContext context, AuthService auth, CancellationToken token) =>
        {
            await auth.RevokeSessionAsync(ReadBearerToken(context), token);
            return Results.NoContent();
        });
    }

    public static string? ReadBearerToken(HttpContext context)
    {
        var authorization = context.Request.Headers.Authorization.ToString();
        return authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authorization[7..].Trim()
            : null;
    }
}