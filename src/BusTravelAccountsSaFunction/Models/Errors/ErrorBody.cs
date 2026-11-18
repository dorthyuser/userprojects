using System.Text.Json.Serialization;

namespace BusTravelAccountsSaFunction;

public sealed class ErrorBody
{
    [JsonPropertyName("errorCode")]
    public int ErrorCode { get; set; }

    [JsonPropertyName("errorDateTime")]
    public DateTimeOffset ErrorDateTime { get; set; }

    [JsonPropertyName("errorMessage")]
    public string ErrorMessage { get; set; } = string.Empty;

    [JsonPropertyName("errorDescription")]
    public string ErrorDescription { get; set; } = string.Empty;
}