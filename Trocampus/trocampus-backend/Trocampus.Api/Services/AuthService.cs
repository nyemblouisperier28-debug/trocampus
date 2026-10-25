using System.Security.Cryptography;
using System.Text;
using Npgsql;
using Trocampus.Api.Contracts;

namespace Trocampus.Api.Services;

public sealed class AuthService(NpgsqlDataSource dataSource)
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(30);

    public async Task<SessionResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand("""
            SELECT id, first_name, email::text, password_hash, role, status
            FROM users WHERE email = @email
            """, connection);
        command.Parameters.AddWithValue("email", request.Email.Trim());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken) || reader.GetString(5) != "active" ||
            !VerifyPassword(request.Password, reader.GetString(3)))
            return null;

        var userId = reader.GetInt64(0);
        var firstName = reader.GetString(1);
        var email = reader.GetString(2);
        var role = reader.GetString(4);
        await reader.CloseAsync();

        await using (var update = new NpgsqlCommand("UPDATE users SET last_login_at = now() WHERE id = @id", connection))
        {
            update.Parameters.AddWithValue("id", userId);
            await update.ExecuteNonQueryAsync(cancellationToken);
        }

        return await CreateSessionAsync(connection, userId, firstName, email, role, cancellationToken);
    }

    public async Task<SessionResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var token = CreateToken();
        var expiresAt = DateTimeOffset.UtcNow.Add(SessionLifetime);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using var command = new NpgsqlCommand("""
            INSERT INTO users (first_name, email, password_hash, campus_id, zone_id, terms_accepted_at)
            VALUES (@first_name, @email, @password_hash, @campus_id, @zone_id, now())
            RETURNING id, first_name, email::text, role
            """, connection, transaction);
        command.Parameters.AddWithValue("first_name", request.FirstName.Trim());
        command.Parameters.AddWithValue("email", request.Email.Trim());
        command.Parameters.AddWithValue("password_hash", HashPassword(request.Password));
        command.Parameters.AddWithValue("campus_id", (object?)request.CampusId ?? DBNull.Value);
        command.Parameters.AddWithValue("zone_id", (object?)request.ZoneId ?? DBNull.Value);

        long userId;
        string firstName;
        string email;
        string role;
        try
        {
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);
            userId = reader.GetInt64(0);
            firstName = reader.GetString(1);
            email = reader.GetString(2);
            role = reader.GetString(3);
            await reader.CloseAsync();

            await using var settings = new NpgsqlCommand("INSERT INTO user_settings (user_id) VALUES (@user_id)", connection, transaction);
            settings.Parameters.AddWithValue("user_id", userId);
            await settings.ExecuteNonQueryAsync(cancellationToken);

            await InsertSessionAsync(connection, transaction, userId, token, expiresAt, cancellationToken);

            await using (var welcomePoints = new NpgsqlCommand("""
                INSERT INTO points_transactions (user_id, amount, kind, label, status)
                VALUES (@user_id, 50, 'invitation', 'Bonus de bienvenue', 'confirmed')
                """, connection, transaction))
            {
                welcomePoints.Parameters.AddWithValue("user_id", userId);
                await welcomePoints.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var referral = new NpgsqlCommand("""
                UPDATE invitations
                SET invitee_user_id = @user_id, status = 'confirmed', confirmed_at = now()
                WHERE id = (
                    SELECT id FROM invitations
                    WHERE invitee_email = @email AND status = 'pending'
                    ORDER BY created_at LIMIT 1 FOR UPDATE
                )
                RETURNING id, inviter_id
                """, connection, transaction))
            {
                referral.Parameters.AddWithValue("user_id", userId);
                referral.Parameters.AddWithValue("email", email);
                await using var referralReader = await referral.ExecuteReaderAsync(cancellationToken);
                if (await referralReader.ReadAsync(cancellationToken))
                {
                    var invitationId = referralReader.GetInt64(0);
                    var inviterId = referralReader.GetInt64(1);
                    await referralReader.CloseAsync();

                    await using var referralPoints = new NpgsqlCommand("""
                        INSERT INTO points_transactions (user_id, amount, kind, label, status, invitation_id)
                        VALUES (@inviter_id, 10, 'invitation', 'Invitation confirmée', 'confirmed', @invitation_id)
                        ON CONFLICT DO NOTHING
                        """, connection, transaction);
                    referralPoints.Parameters.AddWithValue("inviter_id", inviterId);
                    referralPoints.Parameters.AddWithValue("invitation_id", invitationId);
                    await referralPoints.ExecuteNonQueryAsync(cancellationToken);
                }
            }
            await transaction.CommitAsync(cancellationToken);
        }
        catch (PostgresException exception) when (exception.SqlState == Npgsql.PostgresErrorCodes.UniqueViolation)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        return new SessionResponse(userId, firstName, email, role, token, expiresAt);
    }

    public async Task<long?> GetUserIdAsync(string? bearerToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(bearerToken)) return null;
        var hash = HashToken(bearerToken);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand("""
                        SELECT sessions.user_id
                        FROM user_sessions sessions
                        JOIN users ON users.id = sessions.user_id AND users.status = 'active'
                        WHERE sessions.token_hash = @token_hash
                            AND sessions.expires_at > now()
                            AND sessions.revoked_at IS NULL
            """, connection);
        command.Parameters.AddWithValue("token_hash", hash);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is long userId ? userId : null;
    }

    public async Task RevokeSessionAsync(string? bearerToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(bearerToken)) return;
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "UPDATE user_sessions SET revoked_at = now() WHERE token_hash = @token_hash AND revoked_at IS NULL", connection);
        command.Parameters.AddWithValue("token_hash", HashToken(bearerToken));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<SessionResponse> CreateSessionAsync(NpgsqlConnection connection, long userId,
        string firstName, string email, string role, CancellationToken cancellationToken)
    {
        var token = CreateToken();
        var expiresAt = DateTimeOffset.UtcNow.Add(SessionLifetime);
        await InsertSessionAsync(connection, null, userId, token, expiresAt, cancellationToken);
        return new SessionResponse(userId, firstName, email, role, token, expiresAt);
    }

    private static async Task InsertSessionAsync(NpgsqlConnection connection, NpgsqlTransaction? transaction,
        long userId, string token, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("""
            INSERT INTO user_sessions (user_id, token_hash, expires_at)
            VALUES (@user_id, @token_hash, @expires_at)
            """, connection, transaction);
        command.Parameters.AddWithValue("user_id", userId);
        command.Parameters.AddWithValue("token_hash", HashToken(token));
        command.Parameters.AddWithValue("expires_at", expiresAt);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string CreateToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string HashPassword(string password)
    {
        const int iterations = 600_000;
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, 32);
        return $"pbkdf2-sha256${iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split('$');
        if (parts.Length != 4 || parts[0] != "pbkdf2-sha256" ||
            !int.TryParse(parts[1], out var iterations) || iterations < 100_000 || iterations > 2_000_000)
            return false;
        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}