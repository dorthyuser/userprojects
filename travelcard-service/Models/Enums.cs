using System.Text.Json.Serialization;

namespace travelcard_service.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TravelPurpose
    {
        Business,
        Leisure
    }
}
