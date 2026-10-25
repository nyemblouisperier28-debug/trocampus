using System.ComponentModel.DataAnnotations;

namespace Trocampus.Api.Contracts;

public sealed record RegisterRequest
{
    [Required, StringLength(60, MinimumLength = 1)]
    public required string FirstName { get; init; }

    [Required, EmailAddress, StringLength(254)]
    public required string Email { get; init; }

    [Required, StringLength(128, MinimumLength = 10)]
    public required string Password { get; init; }

    [Range(1, long.MaxValue)]
    public long? CampusId { get; init; }

    [Range(1, long.MaxValue)]
    public long? ZoneId { get; init; }
}

public sealed record LoginRequest
{
    [Required, EmailAddress]
    public required string Email { get; init; }

    [Required]
    public required string Password { get; init; }
}

public sealed record SessionResponse(long UserId, string FirstName, string Email, string Role, string AccessToken, DateTimeOffset ExpiresAt);

public sealed record CurrentUserResponse(long UserId, string FirstName, string? LastName, string Email,
    string Role, string Campus, string Zone, int Points, double AverageRating, int ReviewCount,
    int ExchangesCompleted);

public sealed record CreateListingRequest
{
    [Range(1, long.MaxValue)]
    public long CategoryId { get; init; }

    [Range(1, long.MaxValue)]
    public long ZoneId { get; init; }

    [Required, StringLength(120, MinimumLength = 3)]
    public required string Title { get; init; }

    [StringLength(5000)]
    public string? Description { get; init; }

    [RegularExpression("^(neuf|bon_etat|usage)$")]
    public string? Condition { get; init; }

    [Required, RegularExpression("^(sale|exchange)$")]
    public required string ListingType { get; init; }

    [Range(0, int.MaxValue)]
    public int? PriceCents { get; init; }

    [Range(0, int.MaxValue)]
    public int? PricePoints { get; init; }

    [StringLength(500)]
    public string? WantedText { get; init; }

    [Required, RegularExpression("^(draft|active)$")]
    public required string Status { get; init; }
}

public sealed record ListingResponse(long Id, string Title, string? Description, string Owner, string Category,
    string Campus, string Zone, string? Condition, string ListingType, int? PriceCents, int? PricePoints,
    string? WantedText, DateTimeOffset CreatedAt);

public sealed record HomeSummaryResponse(int Points, int PointsEarned, int PointsSpent, int MessagesUnread,
    int OffersReceived, int ExchangesCompleted);

public sealed record CategoryResponse(long Id, string Name);
public sealed record ZoneResponse(long Id, string Name, string Campus);

public sealed record CreateOfferRequest
{
    [Range(1, long.MaxValue)]
    public long ListingId { get; init; }

    [Range(1, long.MaxValue)]
    public long? OfferedListingId { get; init; }

    [Range(0, int.MaxValue)]
    public int OfferedPoints { get; init; }

    [Range(0, int.MaxValue)]
    public int OfferedCents { get; init; }

    [StringLength(2000)]
    public string? Message { get; init; }
}

public sealed record OfferResponse(long Id, long ListingId, string ListingTitle, string OtherParty,
    int OfferedPoints, int OfferedCents, string? Message, string Status, DateTimeOffset CreatedAt);

public sealed record OfferDecisionRequest
{
    [Required, RegularExpression("^(accepted|refused)$")]
    public required string Status { get; init; }
}

public sealed record ConversationResponse(long Id, string OtherParty, string ListingTitle,
    string? LastMessage, DateTimeOffset? LastMessageAt, int UnreadCount);

public sealed record MessageResponse(long Id, long ConversationId, long SenderId,
    string Sender, string Body, DateTimeOffset SentAt);

public sealed record SendMessageRequest
{
    [Required, StringLength(2000, MinimumLength = 1)]
    public required string Body { get; init; }
}

public sealed record PointTransactionResponse(long Id, int Amount, string Kind, string Label,
    string Status, DateTimeOffset CreatedAt);

public sealed record CreateInvitationRequest
{
    [Required, EmailAddress, StringLength(254)]
    public required string Email { get; init; }
}

public sealed record ExchangeHistoryResponse(long Id, string Outcome, string ListingTitle,
    string OtherParty, DateTimeOffset CompletedAt, string? FailureNote);

public sealed record AppointmentResponse(long Id, string ListingTitle, string OtherParty,
    string Place, DateTimeOffset ScheduledAt);

public sealed record ConfirmExchangeRequest
{
    [Required, RegularExpression("^(success|failed)$")]
    public required string Outcome { get; init; }

    [StringLength(500)]
    public string? FailureNote { get; init; }
}