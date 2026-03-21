using System.Text.Json.Serialization;
using System.Text.Json;
using System;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace Ddctravelcard2026Lambda.Models
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

    // Helper for JSON serialization if customization required later
    public static class EnumJsonOptions
    {
        public static JsonSerializerOptions ExactStringEnumOptions()
        {
            var opts = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
            };
            opts.Converters.Add(new JsonStringEnumConverter());
            return opts;
        }
    }
}
