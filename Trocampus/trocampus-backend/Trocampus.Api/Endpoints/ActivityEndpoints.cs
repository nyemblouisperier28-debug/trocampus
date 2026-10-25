using Npgsql;
using Trocampus.Api.Contracts;
using Trocampus.Api.Services;

namespace Trocampus.Api.Endpoints;

public static class ActivityEndpoints
{
    public static void MapActivityEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api").WithTags("Activity");

        api.MapPost("/offers", async (CreateOfferRequest request, HttpContext context, AuthService auth,
            NpgsqlDataSource dataSource, CancellationToken token) =>
        {
            var userId = await auth.GetUserIdAsync(AuthEndpoints.ReadBearerToken(context), token);
            if (userId is null) return Results.Unauthorized();
            if (request.OfferedPoints == 0 && request.OfferedCents == 0 && request.OfferedListingId is null)
                return Results.BadRequest(new { message = "Proposez un objet, des points ou un montant." });

            await using var connection = await dataSource.OpenConnectionAsync(token);
            await using var transaction = await connection.BeginTransactionAsync(token);
            if (request.OfferedListingId is long offeredListingId)
            {
                await using var offeredOwnership = new NpgsqlCommand(
                    "SELECT EXISTS (SELECT 1 FROM listings WHERE id = @listing_id AND owner_id = @user_id AND status = 'active')",
                    connection, transaction);
                offeredOwnership.Parameters.AddWithValue("listing_id", offeredListingId);
                offeredOwnership.Parameters.AddWithValue("user_id", userId.Value);
                if (!((bool)(await offeredOwnership.ExecuteScalarAsync(token) ?? false)))
                    return Results.BadRequest(new { message = "L’objet proposé doit être une annonce active qui vous appartient." });
            }
            await using var command = new NpgsqlCommand("""
                INSERT INTO offers (listing_id, proposer_id, offered_listing_id, offered_points,
                                    offered_cents, message)
                SELECT l.id, @user_id, @offered_listing_id, @points, @cents, @message
                FROM listings l
                WHERE l.id = @listing_id AND l.status = 'active' AND l.owner_id <> @user_id
                RETURNING id
                """, connection, transaction);
            command.Parameters.AddWithValue("user_id", userId.Value);
            command.Parameters.AddWithValue("listing_id", request.ListingId);
            command.Parameters.AddWithValue("offered_listing_id", (object?)request.OfferedListingId ?? DBNull.Value);
            command.Parameters.AddWithValue("points", request.OfferedPoints);
            command.Parameters.AddWithValue("cents", request.OfferedCents);
            command.Parameters.AddWithValue("message", (object?)request.Message?.Trim() ?? DBNull.Value);
            long offerId;
            try
            {
                var result = await command.ExecuteScalarAsync(token);
                if (result is not long id) return Results.NotFound(new { message = "Annonce introuvable ou indisponible." });
                offerId = id;
            }
            catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                return Results.Conflict(new { message = "Vous avez déjà une proposition en attente pour cette annonce." });
            }

            await using (var conversation = new NpgsqlCommand("""
                WITH created AS (
                    INSERT INTO conversations (listing_id, offer_id) VALUES (@listing_id, @offer_id)
                    RETURNING id
                )
                INSERT INTO conversation_participants (conversation_id, user_id)
                SELECT created.id, participant.user_id
                FROM created
                CROSS JOIN (VALUES (@owner_id), (@user_id)) AS participant(user_id)
                """, connection, transaction))
            {
                conversation.Parameters.AddWithValue("listing_id", request.ListingId);
                conversation.Parameters.AddWithValue("offer_id", offerId);
                conversation.Parameters.AddWithValue("owner_id", await GetListingOwnerAsync(connection, transaction, request.ListingId, token));
                conversation.Parameters.AddWithValue("user_id", userId.Value);
                await conversation.ExecuteNonQueryAsync(token);
            }

            await transaction.CommitAsync(token);
            return Results.Created($"/api/offers/{offerId}", new { Id = offerId });
        });

        api.MapGet("/offers", async (HttpContext context, AuthService auth, NpgsqlDataSource dataSource,
            CancellationToken token) =>
        {
            var userId = await auth.GetUserIdAsync(AuthEndpoints.ReadBearerToken(context), token);
            if (userId is null) return Results.Unauthorized();
            await using var connection = await dataSource.OpenConnectionAsync(token);
            await using var command = new NpgsqlCommand("""
                SELECT o.id, l.id, l.title, u.first_name, o.offered_points, o.offered_cents,
                       o.message, o.status, o.created_at
                FROM offers o JOIN listings l ON l.id = o.listing_id
                JOIN users u ON u.id = CASE WHEN o.proposer_id = @user_id THEN l.owner_id ELSE o.proposer_id END
                WHERE o.proposer_id = @user_id OR l.owner_id = @user_id
                ORDER BY o.created_at DESC
                """, connection);
            command.Parameters.AddWithValue("user_id", userId.Value);
            var offers = new List<OfferResponse>();
            await using var reader = await command.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token))
                offers.Add(new OfferResponse(reader.GetInt64(0), reader.GetInt64(1), reader.GetString(2),
                    reader.GetString(3), reader.GetInt32(4), reader.GetInt32(5), reader.IsDBNull(6) ? null : reader.GetString(6),
                    reader.GetString(7), reader.GetFieldValue<DateTimeOffset>(8)));
            return Results.Ok(offers);
        });

        api.MapPost("/offers/{offerId:long}/decision", async (long offerId, OfferDecisionRequest request,
            HttpContext context, AuthService auth, NpgsqlDataSource dataSource, CancellationToken token) =>
        {
            var userId = await auth.GetUserIdAsync(AuthEndpoints.ReadBearerToken(context), token);
            if (userId is null) return Results.Unauthorized();
            await using var connection = await dataSource.OpenConnectionAsync(token);
            await using var transaction = await connection.BeginTransactionAsync(token);
            await using var lockCommand = new NpgsqlCommand("""
                SELECT o.proposer_id, l.owner_id, o.offered_points, l.title
                FROM offers o JOIN listings l ON l.id = o.listing_id
                                WHERE o.id = @offer_id AND l.owner_id = @user_id
                                    AND l.status = 'active' AND o.status = 'pending'
                FOR UPDATE OF o, l
                """, connection, transaction);
            lockCommand.Parameters.AddWithValue("offer_id", offerId);
            lockCommand.Parameters.AddWithValue("user_id", userId.Value);
            long proposerId;
            int points;
            string title;
            await using (var reader = await lockCommand.ExecuteReaderAsync(token))
            {
                if (!await reader.ReadAsync(token)) return Results.NotFound();
                proposerId = reader.GetInt64(0);
                points = reader.GetInt32(2);
                title = reader.GetString(3);
            }

            if (request.Status == "accepted" && points > 0)
            {
                await using var balanceLock = new NpgsqlCommand("SELECT id FROM users WHERE id IN (@owner, @proposer) ORDER BY id FOR UPDATE", connection, transaction);
                balanceLock.Parameters.AddWithValue("owner", userId.Value);
                balanceLock.Parameters.AddWithValue("proposer", proposerId);
                await balanceLock.ExecuteNonQueryAsync(token);
                await using var balance = new NpgsqlCommand("""
                    SELECT balance + COALESCE((SELECT SUM(amount) FROM points_transactions
                        WHERE user_id = @user_id AND status = 'pending' AND amount < 0), 0)
                    FROM v_user_points WHERE user_id = @user_id
                    """, connection, transaction);
                balance.Parameters.AddWithValue("user_id", proposerId);
                var available = (long)(await balance.ExecuteScalarAsync(token) ?? 0L);
                if (available < points) return Results.Conflict(new { message = "Solde de points insuffisant." });
                await using var debit = new NpgsqlCommand("""
                    INSERT INTO points_transactions (user_id, amount, kind, label, status, offer_id)
                    VALUES (@user_id, -@points, 'exchange', @label, 'pending', @offer_id)
                    """, connection, transaction);
                debit.Parameters.AddWithValue("user_id", proposerId);
                debit.Parameters.AddWithValue("points", points);
                debit.Parameters.AddWithValue("label", $"Échange · {title}");
                debit.Parameters.AddWithValue("offer_id", offerId);
                await debit.ExecuteNonQueryAsync(token);
            }

            await using (var update = new NpgsqlCommand("""
                UPDATE offers SET status = @status, responded_at = now() WHERE id = @offer_id AND status = 'pending'
                """, connection, transaction))
            {
                update.Parameters.AddWithValue("status", request.Status);
                update.Parameters.AddWithValue("offer_id", offerId);
                await update.ExecuteNonQueryAsync(token);
            }
            if (request.Status == "accepted")
            {
                await using var reserve = new NpgsqlCommand("UPDATE listings SET status = 'reserved' WHERE id = (SELECT listing_id FROM offers WHERE id = @offer_id)", connection, transaction);
                reserve.Parameters.AddWithValue("offer_id", offerId);
                await reserve.ExecuteNonQueryAsync(token);
                await using var appointment = new NpgsqlCommand("""
                    INSERT INTO appointments (offer_id, place, scheduled_at, proposed_by)
                    VALUES (@offer_id, 'À convenir sur le campus', now() + interval '7 days', @user_id)
                    ON CONFLICT (offer_id) DO NOTHING
                    """, connection, transaction);
                appointment.Parameters.AddWithValue("offer_id", offerId);
                appointment.Parameters.AddWithValue("user_id", userId.Value);
                await appointment.ExecuteNonQueryAsync(token);
            }
            await transaction.CommitAsync(token);
            return Results.NoContent();
        });

        api.MapGet("/conversations", async (HttpContext context, AuthService auth, NpgsqlDataSource dataSource,
            CancellationToken token) =>
        {
            var userId = await auth.GetUserIdAsync(AuthEndpoints.ReadBearerToken(context), token);
            if (userId is null) return Results.Unauthorized();
            await using var connection = await dataSource.OpenConnectionAsync(token);
            await using var command = new NpgsqlCommand("""
                SELECT c.id, other.first_name, l.title, latest.body, latest.sent_at, COALESCE(unread.unread, 0)::integer
                FROM conversation_participants cp JOIN conversations c ON c.id = cp.conversation_id
                JOIN listings l ON l.id = c.listing_id
                JOIN conversation_participants other_cp ON other_cp.conversation_id = c.id AND other_cp.user_id <> cp.user_id
                JOIN users other ON other.id = other_cp.user_id
                LEFT JOIN LATERAL (SELECT body, sent_at FROM messages WHERE conversation_id = c.id ORDER BY sent_at DESC, id DESC LIMIT 1) latest ON true
                LEFT JOIN v_unread_messages unread ON unread.conversation_id = c.id AND unread.user_id = cp.user_id
                WHERE cp.user_id = @user_id ORDER BY COALESCE(latest.sent_at, c.created_at) DESC
                """, connection);
            command.Parameters.AddWithValue("user_id", userId.Value);
            var conversations = new List<ConversationResponse>();
            await using var reader = await command.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token))
                conversations.Add(new ConversationResponse(reader.GetInt64(0), reader.GetString(1), reader.GetString(2),
                    reader.IsDBNull(3) ? null : reader.GetString(3), reader.IsDBNull(4) ? null : reader.GetFieldValue<DateTimeOffset>(4), reader.GetInt32(5)));
            return Results.Ok(conversations);
        });

        api.MapGet("/conversations/{conversationId:long}/messages", async (long conversationId,
            HttpContext context, AuthService auth, NpgsqlDataSource dataSource, CancellationToken token) =>
        {
            var userId = await auth.GetUserIdAsync(AuthEndpoints.ReadBearerToken(context), token);
            if (userId is null) return Results.Unauthorized();
            await using var connection = await dataSource.OpenConnectionAsync(token);
            await using var transaction = await connection.BeginTransactionAsync(token);
            await using var access = new NpgsqlCommand("UPDATE conversation_participants SET last_read_at = now() WHERE conversation_id = @conversation_id AND user_id = @user_id", connection, transaction);
            access.Parameters.AddWithValue("conversation_id", conversationId);
            access.Parameters.AddWithValue("user_id", userId.Value);
            if (await access.ExecuteNonQueryAsync(token) == 0) return Results.NotFound();
            await using var command = new NpgsqlCommand("""
                SELECT m.id, m.conversation_id, m.sender_id, u.first_name, m.body, m.sent_at
                FROM messages m JOIN users u ON u.id = m.sender_id
                WHERE m.conversation_id = @conversation_id ORDER BY m.sent_at, m.id
                """, connection, transaction);
            command.Parameters.AddWithValue("conversation_id", conversationId);
            var messages = new List<MessageResponse>();
            await using (var reader = await command.ExecuteReaderAsync(token))
                while (await reader.ReadAsync(token)) messages.Add(new MessageResponse(reader.GetInt64(0), reader.GetInt64(1), reader.GetInt64(2), reader.GetString(3), reader.GetString(4), reader.GetFieldValue<DateTimeOffset>(5)));
            await transaction.CommitAsync(token);
            return Results.Ok(messages);
        });

        api.MapPost("/conversations/{conversationId:long}/messages", async (long conversationId,
            SendMessageRequest request, HttpContext context, AuthService auth, NpgsqlDataSource dataSource,
            CancellationToken token) =>
        {
            var userId = await auth.GetUserIdAsync(AuthEndpoints.ReadBearerToken(context), token);
            if (userId is null) return Results.Unauthorized();
            await using var connection = await dataSource.OpenConnectionAsync(token);
            await using var command = new NpgsqlCommand("""
                INSERT INTO messages (conversation_id, sender_id, body)
                SELECT @conversation_id, @user_id, @body
                WHERE EXISTS (SELECT 1 FROM conversation_participants WHERE conversation_id = @conversation_id AND user_id = @user_id)
                RETURNING id, sent_at
                """, connection);
            command.Parameters.AddWithValue("conversation_id", conversationId);
            command.Parameters.AddWithValue("user_id", userId.Value);
            command.Parameters.AddWithValue("body", request.Body.Trim());
            await using var reader = await command.ExecuteReaderAsync(token);
            if (!await reader.ReadAsync(token)) return Results.NotFound();
            return Results.Created($"/api/conversations/{conversationId}/messages/{reader.GetInt64(0)}", new { Id = reader.GetInt64(0), SentAt = reader.GetFieldValue<DateTimeOffset>(1) });
        });

        api.MapGet("/points/transactions", async (HttpContext context, AuthService auth,
            NpgsqlDataSource dataSource, CancellationToken token) =>
        {
            var userId = await auth.GetUserIdAsync(AuthEndpoints.ReadBearerToken(context), token);
            if (userId is null) return Results.Unauthorized();
            await using var connection = await dataSource.OpenConnectionAsync(token);
            await using var command = new NpgsqlCommand("SELECT id, amount, kind, label, status, created_at FROM points_transactions WHERE user_id = @user_id AND status IN ('confirmed', 'completed') ORDER BY created_at DESC, id DESC", connection);
            command.Parameters.AddWithValue("user_id", userId.Value);
            var rows = new List<PointTransactionResponse>();
            await using var reader = await command.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token)) rows.Add(new PointTransactionResponse(reader.GetInt64(0), reader.GetInt32(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetFieldValue<DateTimeOffset>(5)));
            return Results.Ok(rows);
        });

        api.MapGet("/appointments", async (HttpContext context, AuthService auth,
            NpgsqlDataSource dataSource, CancellationToken token) =>
        {
            var userId = await auth.GetUserIdAsync(AuthEndpoints.ReadBearerToken(context), token);
            if (userId is null) return Results.Unauthorized();
            await using var connection = await dataSource.OpenConnectionAsync(token);
            await using var command = new NpgsqlCommand("""
                SELECT a.id, l.title, other.first_name, a.place, a.scheduled_at
                FROM appointments a JOIN offers o ON o.id = a.offer_id
                JOIN listings l ON l.id = o.listing_id
                JOIN users other ON other.id = CASE WHEN l.owner_id = @user_id THEN o.proposer_id ELSE l.owner_id END
                LEFT JOIN exchange_outcomes eo ON eo.appointment_id = a.id
                WHERE (l.owner_id = @user_id OR o.proposer_id = @user_id)
                                    AND a.status IN ('planned', 'confirmed')
                                    AND (eo.id IS NULL OR (l.owner_id = @user_id AND eo.confirmed_by_owner_at IS NULL)
                                             OR (o.proposer_id = @user_id AND eo.confirmed_by_proposer_at IS NULL))
                ORDER BY a.scheduled_at
                """, connection);
            command.Parameters.AddWithValue("user_id", userId.Value);
            var appointments = new List<AppointmentResponse>();
            await using var reader = await command.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token)) appointments.Add(new AppointmentResponse(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetFieldValue<DateTimeOffset>(4)));
            return Results.Ok(appointments);
        });

        api.MapPost("/appointments/{appointmentId:long}/confirm", async (long appointmentId,
            ConfirmExchangeRequest request, HttpContext context, AuthService auth,
            NpgsqlDataSource dataSource, CancellationToken token) =>
        {
            var userId = await auth.GetUserIdAsync(AuthEndpoints.ReadBearerToken(context), token);
            if (userId is null) return Results.Unauthorized();
            await using var connection = await dataSource.OpenConnectionAsync(token);
            await using var transaction = await connection.BeginTransactionAsync(token);
            await using var locked = new NpgsqlCommand("""
                SELECT a.offer_id, o.proposer_id, l.owner_id, o.offered_points, l.title,
                       (l.owner_id = @user_id)
                FROM appointments a JOIN offers o ON o.id = a.offer_id
                JOIN listings l ON l.id = o.listing_id
                WHERE a.id = @appointment_id
                  AND (l.owner_id = @user_id OR o.proposer_id = @user_id)
                  AND a.status IN ('planned', 'confirmed')
                FOR UPDATE OF a
                """, connection, transaction);
            locked.Parameters.AddWithValue("appointment_id", appointmentId);
            locked.Parameters.AddWithValue("user_id", userId.Value);
            long offerId;
            long proposerId;
            long ownerId;
            int points;
            string title;
            bool isOwner;
            await using (var reader = await locked.ExecuteReaderAsync(token))
            {
                if (!await reader.ReadAsync(token)) return Results.NotFound();
                offerId = reader.GetInt64(0);
                proposerId = reader.GetInt64(1);
                ownerId = reader.GetInt64(2);
                points = reader.GetInt32(3);
                title = reader.GetString(4);
                isOwner = reader.GetBoolean(5);
            }

            await using (var saveConfirmation = new NpgsqlCommand("""
                INSERT INTO exchange_outcomes (appointment_id, outcome, failure_note,
                    confirmed_by_owner_at, confirmed_by_proposer_at)
                VALUES (@appointment_id, @outcome, @failure_note,
                    CASE WHEN @is_owner THEN now() ELSE NULL END,
                    CASE WHEN @is_owner THEN NULL ELSE now() END)
                ON CONFLICT (appointment_id) DO UPDATE SET
                    confirmed_by_owner_at = COALESCE(exchange_outcomes.confirmed_by_owner_at, EXCLUDED.confirmed_by_owner_at),
                    confirmed_by_proposer_at = COALESCE(exchange_outcomes.confirmed_by_proposer_at, EXCLUDED.confirmed_by_proposer_at)
                WHERE exchange_outcomes.outcome = EXCLUDED.outcome
                RETURNING id
                """, connection, transaction))
            {
                saveConfirmation.Parameters.AddWithValue("appointment_id", appointmentId);
                saveConfirmation.Parameters.AddWithValue("outcome", request.Outcome);
                saveConfirmation.Parameters.AddWithValue("failure_note", (object?)request.FailureNote ?? DBNull.Value);
                saveConfirmation.Parameters.AddWithValue("is_owner", isOwner);
                if (await saveConfirmation.ExecuteScalarAsync(token) is null)
                    return Results.Conflict(new { message = "Les deux participants doivent confirmer le même résultat." });
            }

            string? finalOutcome;
            bool mutuallyConfirmed;
            await using (var state = new NpgsqlCommand("""
                SELECT outcome, confirmed_by_owner_at IS NOT NULL AND confirmed_by_proposer_at IS NOT NULL
                FROM exchange_outcomes WHERE appointment_id = @appointment_id
                """, connection, transaction))
            {
                state.Parameters.AddWithValue("appointment_id", appointmentId);
                await using var reader = await state.ExecuteReaderAsync(token);
                await reader.ReadAsync(token);
                finalOutcome = reader.GetString(0);
                mutuallyConfirmed = reader.GetBoolean(1);
            }

            if (mutuallyConfirmed)
            {
                await using (var settle = new NpgsqlCommand("""
                    UPDATE points_transactions SET status = @point_status
                    WHERE offer_id = @offer_id AND user_id = @proposer_id
                      AND amount < 0 AND kind = 'exchange' AND status = 'pending'
                    """, connection, transaction))
                {
                    settle.Parameters.AddWithValue("point_status", finalOutcome == "success" ? "completed" : "cancelled");
                    settle.Parameters.AddWithValue("offer_id", offerId);
                    settle.Parameters.AddWithValue("proposer_id", proposerId);
                    await settle.ExecuteNonQueryAsync(token);
                }
                if (finalOutcome == "success" && points > 0)
                {
                    await using var credit = new NpgsqlCommand("""
                        INSERT INTO points_transactions (user_id, amount, kind, label, status, offer_id)
                        VALUES (@owner_id, @points, 'exchange', @label, 'completed', @offer_id)
                        ON CONFLICT DO NOTHING
                        """, connection, transaction);
                    credit.Parameters.AddWithValue("owner_id", ownerId);
                    credit.Parameters.AddWithValue("points", points);
                    credit.Parameters.AddWithValue("label", $"Échange réussi · {title}");
                    credit.Parameters.AddWithValue("offer_id", offerId);
                    await credit.ExecuteNonQueryAsync(token);
                }
                await using var finish = new NpgsqlCommand("UPDATE appointments SET status = 'done' WHERE id = @id", connection, transaction);
                finish.Parameters.AddWithValue("id", appointmentId);
                await finish.ExecuteNonQueryAsync(token);
                if (finalOutcome == "failed")
                {
                    await using var release = new NpgsqlCommand("UPDATE listings SET status = 'active' WHERE id = (SELECT listing_id FROM offers WHERE id = @offer_id)", connection, transaction);
                    release.Parameters.AddWithValue("offer_id", offerId);
                    await release.ExecuteNonQueryAsync(token);
                }
            }
            await transaction.CommitAsync(token);
            return Results.NoContent();
        });

        api.MapPost("/invitations", async (CreateInvitationRequest request, HttpContext context,
            AuthService auth, NpgsqlDataSource dataSource, CancellationToken token) =>
        {
            var userId = await auth.GetUserIdAsync(AuthEndpoints.ReadBearerToken(context), token);
            if (userId is null) return Results.Unauthorized();
            await using var connection = await dataSource.OpenConnectionAsync(token);
            await using var command = new NpgsqlCommand("""
                INSERT INTO invitations (inviter_id, invitee_email)
                SELECT @user_id, @email WHERE lower(@email) <> (SELECT lower(email::text) FROM users WHERE id = @user_id)
                ON CONFLICT (inviter_id, invitee_email) DO NOTHING RETURNING id
                """, connection);
            command.Parameters.AddWithValue("user_id", userId.Value);
            command.Parameters.AddWithValue("email", request.Email.Trim());
            var id = await command.ExecuteScalarAsync(token);
            return id is long invitationId ? Results.Created($"/api/invitations/{invitationId}", new { Id = invitationId }) : Results.Conflict(new { message = "Invitation déjà envoyée ou adresse invalide." });
        });

        api.MapGet("/history/exchanges", async (HttpContext context, AuthService auth,
            NpgsqlDataSource dataSource, CancellationToken token) =>
        {
            var userId = await auth.GetUserIdAsync(AuthEndpoints.ReadBearerToken(context), token);
            if (userId is null) return Results.Unauthorized();
            await using var connection = await dataSource.OpenConnectionAsync(token);
            await using var command = new NpgsqlCommand("""
                SELECT eo.id, eo.outcome, l.title, other.first_name, eo.completed_at, eo.failure_note
                FROM exchange_outcomes eo JOIN appointments a ON a.id = eo.appointment_id
                JOIN offers o ON o.id = a.offer_id JOIN listings l ON l.id = o.listing_id
                JOIN users other ON other.id = CASE WHEN l.owner_id = @user_id THEN o.proposer_id ELSE l.owner_id END
                                WHERE (l.owner_id = @user_id OR o.proposer_id = @user_id)
                                    AND eo.confirmed_by_owner_at IS NOT NULL AND eo.confirmed_by_proposer_at IS NOT NULL
                ORDER BY eo.completed_at DESC, eo.id DESC
                """, connection);
            command.Parameters.AddWithValue("user_id", userId.Value);
            var rows = new List<ExchangeHistoryResponse>();
            await using var reader = await command.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token)) rows.Add(new ExchangeHistoryResponse(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetFieldValue<DateTimeOffset>(4), reader.IsDBNull(5) ? null : reader.GetString(5)));
            return Results.Ok(rows);
        });
    }

    private static async Task<long> GetListingOwnerAsync(NpgsqlConnection connection,
        NpgsqlTransaction transaction, long listingId, CancellationToken token)
    {
        await using var command = new NpgsqlCommand("SELECT owner_id FROM listings WHERE id = @id", connection, transaction);
        command.Parameters.AddWithValue("id", listingId);
        return (long)(await command.ExecuteScalarAsync(token) ?? throw new InvalidOperationException("Listing disappeared."));
    }
}
