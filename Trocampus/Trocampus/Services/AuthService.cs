using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Trocampus.Models;

namespace Trocampus.Services;

public sealed record SessionResponse(long UserId, string FirstName, string Email, string Role,
    string AccessToken, DateTimeOffset ExpiresAt);

public sealed record CurrentUserResponse(long UserId, string FirstName, string? LastName, string Email,
    string Role, string Campus, string Zone, int Points, double AverageRating, int ReviewCount,
    int ExchangesCompleted);

public sealed record AuthOperationResult(SessionResponse? Session, HttpStatusCode StatusCode,
    string? ErrorMessage = null, bool AccountCreated = false);

public sealed class AuthSession
{
    private const string SessionKey = "trocampus.auth.session";

    public async Task<SessionResponse?> GetSessionAsync()
    {
        var value = await SecureStorage.Default.GetAsync(SessionKey);
        if (string.IsNullOrWhiteSpace(value))
            return null;

        try
        {
            var session = JsonSerializer.Deserialize<SessionResponse>(value);
            if (session is null || session.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                await ClearAsync();
                return null;
            }

            return session;
        }
        catch (JsonException)
        {
            await ClearAsync();
            return null;
        }
    }

    public Task SaveAsync(SessionResponse session) =>
        SecureStorage.Default.SetAsync(SessionKey, JsonSerializer.Serialize(session));

    public Task ClearAsync()
    {
        SecureStorage.Default.Remove(SessionKey);
        return Task.CompletedTask;
    }
}

public sealed class AuthService(HttpClient http, AuthSession sessionStore)
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<AuthOperationResult> LoginAsync(string email, string password,
        CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsJsonAsync("api/auth/login",
            new LoginRequest(email, password), cancellationToken);
        return await ReadSessionAsync(response, cancellationToken, accountCreated: false);
    }

    public async Task<AuthOperationResult> RegisterAsync(string firstName, string email, string password,
        CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsJsonAsync("api/auth/register",
            new RegisterRequest(firstName, email, password), cancellationToken);
        return await ReadSessionAsync(response, cancellationToken, accountCreated: true);
    }

    public async Task<CurrentUserResponse?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var session = await sessionStore.GetSessionAsync();
        if (session is null)
            return null;

        using var request = new HttpRequestMessage(HttpMethod.Get, "api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        using var response = await http.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await sessionStore.ClearAsync();
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CurrentUserResponse>(_jsonOptions, cancellationToken);
    }

    public async Task<HomeSummary?> GetHomeSummaryAsync(CancellationToken cancellationToken = default)
    {
        var session = await sessionStore.GetSessionAsync();
        if (session is null)
            return null;

        using var request = new HttpRequestMessage(HttpMethod.Get, "api/home/summary");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        using var response = await http.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await sessionStore.ClearAsync();
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<HomeSummary>(_jsonOptions, cancellationToken);
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await sessionStore.GetSessionAsync();
            if (session is null)
                return;

            using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/logout");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
            using var response = await http.SendAsync(request, cancellationToken);
        }
        finally
        {
            await sessionStore.ClearAsync();
        }
    }

    private async Task<AuthOperationResult> ReadSessionAsync(HttpResponseMessage response,
        CancellationToken cancellationToken, bool accountCreated)
    {
        if (!response.IsSuccessStatusCode)
            return new AuthOperationResult(null, response.StatusCode,
                await ReadErrorMessageAsync(response, cancellationToken));

        var session = await response.Content.ReadFromJsonAsync<SessionResponse>(_jsonOptions, cancellationToken);
        if (session is null)
            return new AuthOperationResult(null, response.StatusCode,
                "Le serveur a accepté la demande, mais n’a pas renvoyé de session.", accountCreated);

        try
        {
            await sessionStore.SaveAsync(session);
            return new AuthOperationResult(session, response.StatusCode, AccountCreated: accountCreated);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new AuthOperationResult(null, response.StatusCode,
                $"{(accountCreated ? "Le compte a été créé" : "La connexion a réussi")}, mais l’application n’a pas pu enregistrer la session sécurisée ({exception.GetType().Name}). Réessaie ou contacte le support.",
                accountCreated);
        }
    }

    private static async Task<string?> ReadErrorMessageAsync(HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(body))
            return null;

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (TryGetString(root, "message", out var message) ||
                TryGetString(root, "detail", out message))
                return message;

            if (TryGetProperty(root, "errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                var validationMessages = errors.EnumerateObject()
                    .SelectMany(error => error.Value.ValueKind == JsonValueKind.Array
                        ? error.Value.EnumerateArray().Select(item => $"{error.Name}: {item.GetString()}")
                        : [])
                    .ToArray();
                if (validationMessages.Length > 0)
                    return string.Join(Environment.NewLine, validationMessages);
            }

            if (TryGetString(root, "title", out var title))
                return title;
        }
        catch (JsonException)
        {
        }

        return null;
    }

    private static bool TryGetString(JsonElement element, string name, out string value)
    {
        value = string.Empty;
        return TryGetProperty(element, name, out var property) &&
            property.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(value = property.GetString()!);
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement property)
    {
        foreach (var candidate in element.EnumerateObject())
        {
            if (string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                property = candidate.Value;
                return true;
            }
        }

        property = default;
        return false;
    }

    private sealed record LoginRequest(string Email, string Password);
    private sealed record RegisterRequest(string FirstName, string Email, string Password);
}
