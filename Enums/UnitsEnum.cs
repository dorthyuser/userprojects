using System.Text.Json.Serialization;

namespace AgeApi.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum UnitsEnum
    {
        Days,
        Weeks,
        Minutes,
        Seconds
    }
}
