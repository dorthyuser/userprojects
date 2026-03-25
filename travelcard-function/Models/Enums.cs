using System.Text.Json.Serialization;
using System.Text.Json;

namespace TravelcardFunction.Models
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

    public static class EnumHelpers
    {
        // Placeholder MapEnum method to satisfy project requirements. Consumers may wire this into Npgsql if needed.
        public static void MapEnum<T>(object builder, object translator)
        {
            // Intentionally left as placeholder to document mapping intent in code.
        }

        public class ExactNameTranslator
        {
            // Placeholder translator - preserves exact enum names when mapping to PostgreSQL enums.
        }
    }
}
