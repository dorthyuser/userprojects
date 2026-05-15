using System.Text.Json.Serialization;

namespace Travelcarddemo1251Lambda;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TravelcardTypeEnum
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
public enum CardholderTypeEnum
{
    Primary,
    Secondary
}