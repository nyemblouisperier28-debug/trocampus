namespace Trocampus.Models;

public sealed record ListingResponse(long Id, string Title, string? Description, string Owner, string Category,
    string Campus, string Zone, string? Condition, string ListingType, int? PriceCents, int? PricePoints,
    string? WantedText, DateTimeOffset CreatedAt);

public sealed record CategoryResponse(long Id, string Name);
public sealed record ZoneResponse(long Id, string Name, string Campus);
public sealed record CreatedId(long Id);
public sealed record CreateListingPayload(long CategoryId, long ZoneId, string Title, string? Description,
    string? Condition, string ListingType, int? PriceCents, int? PricePoints, string? WantedText, string Status);
public sealed record OfferResponse(long Id, long ListingId, string ListingTitle, string OtherParty,
    int OfferedPoints, int OfferedCents, string? Message, string Status, DateTimeOffset CreatedAt);
public sealed record ConversationResponse(long Id, string OtherParty, string ListingTitle,
    string? LastMessage, DateTimeOffset? LastMessageAt, int UnreadCount);
public sealed record MessageResponse(long Id, long ConversationId, long SenderId, string Sender,
    string Body, DateTimeOffset SentAt);
public sealed record PointTransactionResponse(long Id, int Amount, string Kind, string Label,
    string Status, DateTimeOffset CreatedAt);
public sealed record ExchangeHistoryResponse(long Id, string Outcome, string ListingTitle,
    string OtherParty, DateTimeOffset CompletedAt, string? FailureNote);
public sealed record AppointmentResponse(long Id, string ListingTitle, string OtherParty,
    string Place, DateTimeOffset ScheduledAt);