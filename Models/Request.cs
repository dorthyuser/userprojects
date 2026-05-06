using System.Text.Json.Serialization;
using TestCsharpLambTc20260506Lambda.Enums;

namespace TestCsharpLambTc20260506Lambda.Models;

public sealed class Request
{
    public TravelcardTypeEnum TravelcardType { get; set; }
    public DateTime TravelcardValidFrom { get; set; }
    public DateTime TravelcardValidTo { get; set; }
    public string? TravelcardName { get; set; }
    public string TravelcardNumber { get; set; } = string.Empty;
    public DateTime TravelcardRequestedDate { get; set; }
    public string TravelcardTransactionReference { get; set; } = string.Empty;
    public DateTime? TravelcardUsableTo { get; set; }
    public List<CardholderRequest> Cardholders { get; set; } = new();
}

public sealed class CardholderRequest
{
    public string CardholderTitle { get; set; } = string.Empty;
    public string CardholderForename { get; set; } = string.Empty;
    public string CardholderSurname { get; set; } = string.Empty;
    public CardholderTypeEnum CardholderType { get; set; }
    public string CardholderPhotoName { get; set; } = string.Empty;
    public string? CardholderPhotoRRSKey { get; set; }
    public string? CardholderPhotoURL { get; set; }
    public string? CardholderPhotoKey { get; set; }
}