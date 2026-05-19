using System.Text.Json.Serialization;

namespace TravelCardFunctionApp.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum travelcard_type_enum
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
public enum cardholder_type_enum
{
    Primary,
    Secondary
}
