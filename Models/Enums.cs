using System.Text.Json.Serialization;

namespace travelcard_function_app.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TravelcardType
{
    Young,
    TwoTogether,
    Family,
    Senior,
    Network,
    TwentySixToThirty,
    SixteenToSeventeen,
    Veterans
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CardholderType
{
    Primary,
    Secondary
}
