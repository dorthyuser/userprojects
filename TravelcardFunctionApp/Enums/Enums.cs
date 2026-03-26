using System.Text.Json.Serialization;

namespace TravelcardFunctionApp.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CardholderType
    {
        Primary,
        Secondary
    }

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
}
