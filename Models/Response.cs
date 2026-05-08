using System.Text.Json.Serialization;

namespace DemoTravelcardClincalLambda.Models;

public sealed class Response
{
    [JsonPropertyName("travelcardId")]
    public string TravelcardId { get; set; } = null!;

    [JsonPropertyName("token")]
    public string Token { get; set; } = null!;
}

public sealed class ErrorResponseModel
{
    [JsonPropertyName("message")]
    public string Message { get; set; }

    [JsonPropertyName("errors")]
    public List<string>? Errors { get; set; }

    public ErrorResponseModel(string message)
    {
        Message = message;
    }
}

public sealed class CardholderRequest
{
    [JsonPropertyName("cardholderTitle")]
    public string CardholderTitle { get; set; } = null!;

    [JsonPropertyName("cardholderForename")]
    public string CardholderForename { get; set; } = null!;

    [JsonPropertyName("cardholderSurname")]
    public string CardholderSurname { get; set; } = null!;

    [JsonPropertyName("cardholderType")]
    public DemoTravelcardClincalLambda.Enums.CardholderType CardholderType { get; set; }

    [JsonPropertyName("cardholderPhotoName")]
    public string CardholderPhotoName { get; set; } = null!;

    [JsonPropertyName("cardholderPhotoRRSKey")]
    public string? CardholderPhotoRRSKey { get; set; }

    [JsonPropertyName("cardholderPhotoURL")]
    public string? CardholderPhotoURL { get; set; }

    [JsonPropertyName("cardholderPhotoKey")]
    public string? CardholderPhotoKey { get; set; }
}