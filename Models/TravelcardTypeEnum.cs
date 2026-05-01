using System.Text.Json.Serialization;

namespace demo_travelcard_paul.Models;

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