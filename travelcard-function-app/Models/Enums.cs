using System.Text.Json.Serialization;

namespace TravelcardFunctionApp.Models
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

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum GrantType
    {
        client_credentials
    }
}
