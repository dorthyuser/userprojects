using System.Text.Json.Serialization;

namespace Travelcardcsharplambda349Lambda.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CardholderTypeEnum
{
    Primary,
    Secondary
}