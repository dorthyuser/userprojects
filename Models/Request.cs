using TestCsharpLambdaTc123Lambda.Services;

namespace TestCsharpLambdaTc123Lambda.Models;

public sealed class Request
{
    public TravelcardType TravelcardType { get; set; }
    public DateTimeOffset TravelcardValidFrom { get; set; }
    public DateTimeOffset TravelcardValidTo { get; set; }
    public string? TravelcardName { get; set; }
    public string TravelcardNumber { get; set; } = string.Empty;
    public DateTimeOffset TravelcardRequestedDate { get; set; }
    public string TravelcardTransactionReference { get; set; } = string.Empty;
    public DateTimeOffset? TravelcardUsableTo { get; set; }
    public List<Cardholder> Cardholders { get; set; } = new();
}

public sealed class Cardholder
{
    public string CardholderTitle { get; set; } = string.Empty;
    public string CardholderForename { get; set; } = string.Empty;
    public string CardholderSurname { get; set; } = string.Empty;
    public CardholderType CardholderType { get; set; }
    public string CardholderPhotoName { get; set; } = string.Empty;
    public string? CardholderPhotoRRSKey { get; set; }
    public string? CardholderPhotoURL { get; set; }
    public string? CardholderPhotoKey { get; set; }
}