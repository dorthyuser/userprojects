using System.Text.Json.Serialization;

namespace BusTravelAccountsSaMainLambda;

public sealed class ErrorDetail
{
    [JsonPropertyName("errorCode")]
    public int ErrorCode { get; set; }

    [JsonPropertyName("errorDateTime")]
    public DateTimeOffset ErrorDateTime { get; set; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("errorDescription")]
    public string? ErrorDescription { get; set; }
}