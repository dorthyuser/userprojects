namespace DemotravelcardsLambda;

public enum TravelcardType
{
    Young,
    TwoTogether,
    Family,
    Senior,
    Network,
    TwentySixToThirty,
    SixteenToSeventeen,
    Veterans
}

public enum CardholderType
{
    Primary,
    Secondary
}

public sealed class CreateTravelcardRequest
{
    public string TravelcardType { get; set; } = string.Empty;
    public DateTimeOffset TravelcardValidFrom { get; set; }
    public DateTimeOffset TravelcardValidTo { get; set; }
    public string? TravelcardName { get; set; }
    public string TravelcardNumber { get; set; } = string.Empty;
    public DateTimeOffset TravelcardRequestedDate { get; set; }
    public string TravelcardTransactionReference { get; set; } = string.Empty;
    public DateTimeOffset? TravelcardUsableTo { get; set; }
    public List<CreateCardholderRequest> Cardholders { get; set; } = new();
}

public sealed class CreateCardholderRequest
{
    public string CardholderTitle { get; set; } = string.Empty;
    public string CardholderForename { get; set; } = string.Empty;
    public string CardholderSurname { get; set; } = string.Empty;
    public string CardholderType { get; set; } = string.Empty;
    public string CardholderPhotoName { get; set; } = string.Empty;
    public string? CardholderPhotoRRSKey { get; set; }
    public string? CardholderPhotoURL { get; set; }
    public string? CardholderPhotoKey { get; set; }
}

public sealed class CreateTravelcardResponse
{
    public string TravelcardId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}