using System.Text.Json;
using System.Text.Json.Serialization;

namespace azuretravelcardfunction309.Models;

public static class JsonOptionsFactory
{
    public static JsonSerializerOptions Options { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            PropertyNameCaseInsensitive = true
        };

        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
