using System.Text.Json.Serialization;

namespace travelcard_http.Models
{
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
}
