using System.Text.Json.Serialization;

namespace TravelcardApi.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Protocol
    {
        HTTPS
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AuthMethod
    {
        oauth2
    }
}
