using System.Text.Json;
using System.Text.Json.Serialization;

namespace BusTravelAccountsEaMainFunction;

public static class JsonOptionsHelper
{
    public static JsonSerializerOptions Create()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }
}