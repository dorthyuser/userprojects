using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TcLambdaLambda.Models
{
    // Custom exact-case enum converter: uses exact enum name matching (case-sensitive)
    public class ExactCaseEnumConverter<T> : JsonConverter<T> where T : struct, Enum
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("Enum value must be a string");
            }
            var text = reader.GetString();
            if (text is null)
                throw new JsonException("Enum value is null");

            if (Enum.TryParse<T>(text, ignoreCase: false, out var value))
            {
                return value;
            }
            throw new JsonException($"Unknown enum value '{text}' for enum type {typeof(T).Name}");
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    // Example enum used in models
    [JsonConverter(typeof(ExactCaseEnumConverter<TravelcardType>))]
    public enum TravelcardType
    {
        Single,
        Return,
        Week,
        Month,
        Annual
    }

    public static class EnumHelpers
    {
        // MapEnum placeholder for systems that require mapping to DB or external libs.
        // Example signature mirrors the requested usage (builder.MapEnum<TravelcardType>("travelcard_type_enum", new ExactNameTranslator()));
        public static void MapEnum<T>(string name, Func<string, string> translator) where T : struct, Enum
        {
            // Placeholder: In a real DB mapping implementation you'd register mapping here.
            Console.WriteLine($"[EnumHelpers] MapEnum called for {typeof(T).Name} as {name}");
        }

        // ExactNameTranslator returns a function that translates enum member to exact name (identity)
        public static Func<string, string> ExactNameTranslator()
        {
            return s => s; // identity translator to preserve exact enum names
        }
    }
}
