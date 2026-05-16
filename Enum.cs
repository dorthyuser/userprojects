using System.Text.Json.Serialization;

namespace DemoTravelcardsLambda;

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