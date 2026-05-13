using System.Text.Json.Serialization;

namespace azurecsharppost1112;

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
