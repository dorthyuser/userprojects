using System.Text.Json;
using System.Text.Json.Serialization;

namespace Travelcardchsarplambda1050Lambda.Services;

public static class JsonOptionsFactory
{
    public static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        return options;
    }
}