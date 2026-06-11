using System.Text.Json.Serialization;

namespace create_travelcard_prod.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TravelcardType
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
public enum CardholderType
{
    Primary,
    Secondary
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserStatus
{
    ACTIVE,
    INACTIVE,
    SUSPENDED
}
