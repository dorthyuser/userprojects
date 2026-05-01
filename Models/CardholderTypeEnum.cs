using System.Text.Json.Serialization;

namespace demo_travelcard_paul.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CardholderTypeEnum
{
    Primary,
    Secondary
}