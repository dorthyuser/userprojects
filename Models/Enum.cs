using System.Text.Json.Serialization;

namespace TravelcardchsarplambdaLambda.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TravelcardTypeEnum
{
    Young,
    Barcklays,
    DevonandCornwall,
    TwoTogether,
    Family,
    Senior,
    DisabledPersons,
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