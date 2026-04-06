using System.Text.Json.Serialization;

namespace TravelcardService.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TravelcardType
    {
        Standard,
        Express
    }
}
