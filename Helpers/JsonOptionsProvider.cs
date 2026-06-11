using System.Text.Json;
using System.Text.Json.Serialization;

namespace life_time_calculator.Helpers;

public static class JsonOptionsProvider
{
    public static JsonSerializerOptions Options { get; } = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };
}
