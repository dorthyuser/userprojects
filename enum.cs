using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Npgsql;
using Npgsql.NameTranslation;

namespace UpTransportTicketApiLambda.Models
{
    // Enum representing categories. JSON uses exact strings like "student", "business-man", "employee", "staff"
    [JsonConverter(typeof(ExactEnumCaseJsonConverter<Category>))]
    public enum Category
    {
        Student,
        BusinessMan,
        Employee,
        Staff
    }

    // Custom JSON converter factory to map exact enum text values used in API
    public class ExactEnumCaseJsonConverter<T> : JsonConverter<T> where T : struct, Enum
    {
        private static readonly Dictionary<string, T> _fromString;
        private static readonly Dictionary<T, string> _toString;

        static ExactEnumCaseJsonConverter()
        {
            _fromString = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
            _toString = new Dictionary<T, string>();

            // Define exact mappings
            Map(Category.Student, "student");
            Map(Category.BusinessMan, "business-man");
            Map(Category.Employee, "employee");
            Map(Category.Staff, "staff");

            static void Map(Category enumVal, string s)
            {
                _fromString[s] = (T)(object)enumVal;
                _toString[(T)(object)enumVal] = s;
            }
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String) throw new JsonException("Expected string for enum value");
            var s = reader.GetString() ?? string.Empty;
            if (_fromString.TryGetValue(s, out var val)) return val;
            throw new JsonException($"Unknown enum value '{s}' for {typeof(T).Name}");
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            if (_toString.TryGetValue(value, out var s)) writer.WriteStringValue(s);
            else writer.WriteStringValue(value.ToString());
        }
    }

    // ExactNameTranslator used for mapping enum names to DB enum names (keeps exact mapping)
    public class ExactNameTranslator : INpgsqlNameTranslator
    {
        public string TranslateMemberName(string name)
        {
            // Map C# enum member names to database enum member names exactly as expected
            return name switch
            {
                nameof(Category.Student) => "student",
                nameof(Category.BusinessMan) => "business-man",
                nameof(Category.Employee) => "employee",
                nameof(Category.Staff) => "staff",
                _ => name
            };
        }

        public string TranslateTypeName(string name)
        {
            // For enum type name
            return name;
        }
    }
}
