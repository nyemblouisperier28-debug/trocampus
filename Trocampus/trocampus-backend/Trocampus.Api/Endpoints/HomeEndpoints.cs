using Npgsql;
using Trocampus.Api.Contracts;
using Trocampus.Api.Services;

namespace Trocampus.Api.Endpoints;

public static class HomeEndpoints
{
    public static void MapHomeEndpoints(this WebApplication app)
    {
        app.MapGet("/api/home/summary", async (HttpContext context, AuthService auth,
            NpgsqlDataSource dataSource, CancellationToken token) =>
        {
            var userId = await auth.GetUserIdAsync(AuthEndpoints.ReadBearerToken(context), token);
            if (userId is null) return Results.Unauthorized();

            await using var connection = await dataSource.OpenConnectionAsync(token);
            await using var command = new NpgsqlCommand("""
                  SELECT COALESCE(points.balance, 0)::integer,
                      COALESCE(points.earned, 0)::integer,
                      COALESCE(points.spent, 0)::integer,
                       COALESCE((SELECT SUM(unread) FROM v_unread_messages WHERE user_id = @user_id), 0)::integer,
                       (SELECT COUNT(*) FROM offers o JOIN listings l ON l.id = o.listing_id
                        WHERE l.owner_id = @user_id AND o.status = 'pending')::integer,
                       (SELECT COUNT(*) FROM exchange_outcomes eo JOIN appointments a ON a.id = eo.appointment_id
                        JOIN offers o ON o.id = a.offer_id JOIN listings l ON l.id = o.listing_id
                        WHERE eo.outcome = 'success' AND (l.owner_id = @user_id OR o.proposer_id = @user_id))::integer
                FROM (SELECT @user_id::bigint AS user_id) user_scope
                LEFT JOIN v_user_points points ON points.user_id = user_scope.user_id
                """, connection);
            command.Parameters.AddWithValue("user_id", userId.Value);
            await using var reader = await command.ExecuteReaderAsync(token);
            await reader.ReadAsync(token);
            return Results.Ok(new HomeSummaryResponse(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2),
                reader.GetInt32(3), reader.GetInt32(4), reader.GetInt32(5)));
        }).WithTags("Home");
    }
}