using System.Text.Json;
using System.Text.Json.Serialization;

namespace BusTravelAccountsSaMainLambda;

public static class JsonOptionsHelper
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}