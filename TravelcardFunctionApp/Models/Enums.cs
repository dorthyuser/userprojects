using System.Text.Json.Serialization;
using Npgsql;

namespace TravelcardFunctionApp.Models
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

    // Translator that preserves exact enum names when mapping to Postgres
    public class ExactNameTranslator : INpgsqlNameTranslator
    {
        public string TranslateMemberName(string memberName)
        {
            return memberName;
        }

        public string TranslateTypeName(string typeName)
        {
            return typeName;
        }
    }
}
