using System;
using System.Text.Json.Serialization;

namespace create_travelcard_prod.Models;

public class CreateTravelcardResponse
{
    [JsonPropertyName("travelcardId")]
    public Guid TravelcardId { get; set; }

    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}
