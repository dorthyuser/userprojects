using System.Text.Json.Serialization;

namespace azuretravelcardfunction309.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum travelcardType_enum
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
public enum cardholderType_enum
{
    Primary,
    Secondary
}
