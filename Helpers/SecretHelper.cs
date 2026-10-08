using System.Text.Json;

namespace BusTravelAccountsSaMainLambda;

public static class SecretHelper
{
    public static string GetSecretText(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property))
        {
            return string.Empty;
        }

        return property.ValueKind == JsonValueKind.String ? property.GetString() ?? string.Empty : property.GetRawText();
    }
}