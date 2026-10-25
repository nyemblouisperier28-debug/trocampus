using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Trocampus.Models;

namespace Trocampus.Services;

public sealed class CampusApiService(HttpClient http, AuthSession sessions)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<IReadOnlyList<ListingResponse>> GetListingsAsync(string? query = null, long? zoneId = null,
        string? type = null, int? radiusMeters = null, CancellationToken cancellationToken = default)
    {
        var parameters = new List<string>();
        if (!string.IsNullOrWhiteSpace(query)) parameters.Add($"q={Uri.EscapeDataString(query.Trim())}");
        if (zoneId is not null) parameters.Add($"zoneId={zoneId.Value}");
        if (radiusMeters is not null) parameters.Add($"radiusM={radiusMeters.Value}");
        if (!string.IsNullOrWhiteSpace(type)) parameters.Add($"type={Uri.EscapeDataString(type)}");
        var path = "api/listings" + (parameters.Count == 0 ? string.Empty : "?" + string.Join("&", parameters));
        return SendAsync<IReadOnlyList<ListingResponse>>(HttpMethod.Get, path, null, cancellationToken);
    }

    public long? CurrentListingId { get; private set; }
    public void SelectListing(long listingId) => CurrentListingId = listingId;

    public Task<IReadOnlyList<CategoryResponse>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<CategoryResponse>>(HttpMethod.Get, "api/categories", null, cancellationToken);

    public Task<IReadOnlyList<ZoneResponse>> GetZonesAsync(CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<ZoneResponse>>(HttpMethod.Get, "api/zones", null, cancellationToken);

    public Task<ListingResponse> CreateListingAsync(CreateListingPayload payload, CancellationToken cancellationToken = default) =>
        SendAsync<ListingResponse>(HttpMethod.Post, "api/listings", payload, cancellationToken);

    public Task<IReadOnlyList<OfferResponse>> GetOffersAsync(CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<OfferResponse>>(HttpMethod.Get, "api/offers", null, cancellationToken);

    public Task<IReadOnlyList<ConversationResponse>> GetConversationsAsync(CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<ConversationResponse>>(HttpMethod.Get, "api/conversations", null, cancellationToken);

    public Task<IReadOnlyList<MessageResponse>> GetMessagesAsync(long conversationId, CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<MessageResponse>>(HttpMethod.Get, $"api/conversations/{conversationId}/messages", null, cancellationToken);

    public Task<CreatedId> SendMessageAsync(long conversationId, string body, CancellationToken cancellationToken = default) =>
        SendAsync<CreatedId>(HttpMethod.Post, $"api/conversations/{conversationId}/messages", new { Body = body }, cancellationToken);

    public Task<IReadOnlyList<PointTransactionResponse>> GetPointTransactionsAsync(CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<PointTransactionResponse>>(HttpMethod.Get, "api/points/transactions", null, cancellationToken);

    public Task<IReadOnlyList<ExchangeHistoryResponse>> GetExchangeHistoryAsync(CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<ExchangeHistoryResponse>>(HttpMethod.Get, "api/history/exchanges", null, cancellationToken);

    public Task<IReadOnlyList<AppointmentResponse>> GetAppointmentsAsync(CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<AppointmentResponse>>(HttpMethod.Get, "api/appointments", null, cancellationToken);

    public Task ConfirmExchangeAsync(long appointmentId, string outcome, string? failureNote = null,
        CancellationToken cancellationToken = default) =>
        SendAsync<object>(HttpMethod.Post, $"api/appointments/{appointmentId}/confirm",
            new { Outcome = outcome, FailureNote = failureNote }, cancellationToken);

    public Task<CreatedId> CreateInvitationAsync(string email, CancellationToken cancellationToken = default) =>
        SendAsync<CreatedId>(HttpMethod.Post, "api/invitations", new { Email = email }, cancellationToken);

    public Task<CreatedId> CreateOfferAsync(long listingId, int points, string? message = null,
        CancellationToken cancellationToken = default) =>
        SendAsync<CreatedId>(HttpMethod.Post, "api/offers", new { ListingId = listingId, OfferedPoints = points, Message = message }, cancellationToken);

    public Task DecideOfferAsync(long offerId, string status, CancellationToken cancellationToken = default) =>
        SendAsync<object>(HttpMethod.Post, $"api/offers/{offerId}/decision", new { Status = status }, cancellationToken);

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body,
        CancellationToken cancellationToken)
    {
        var session = await sessions.GetSessionAsync();
        if (session is null) throw new InvalidOperationException("Une session utilisateur est requise.");
        using var request = new HttpRequestMessage(method, path)
        {
            Content = body is null ? null : JsonContent.Create(body, options: JsonOptions)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        using var response = await http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        if (typeof(T) == typeof(object) || response.Content.Headers.ContentLength == 0)
            return (T)(object)new object();
        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken))
            ?? throw new InvalidOperationException("L’API a renvoyé une réponse vide.");
    }
}
