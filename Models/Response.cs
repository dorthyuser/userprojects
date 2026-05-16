using System.Text.Json.Serialization;

namespace DemoTravelcardsLambda.Models;

public class ResponseDto
{
    [JsonPropertyName("travelcardId")]
    public string TravelcardId { get; set; } = null!;

    [JsonPropertyName("token")]
    public string Token { get; set; } = null!;
}