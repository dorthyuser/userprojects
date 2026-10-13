using System.Text.Json.Serialization;

namespace BusTravelAccountsSaLambda;

public sealed class ErrorResponse
{
    [JsonPropertyName("error")] public ErrorDetails Error { get; set; } = new();
}

public sealed class ErrorDetails
{
    [JsonPropertyName("errorCode")] public int ErrorCode { get; set; }
    [JsonPropertyName("errorDateTime")] public DateTime ErrorDateTime { get; set; }
    [JsonPropertyName("errorMessage")] public string ErrorMessage { get; set; } = string.Empty;
    [JsonPropertyName("errorDescription")] public string ErrorDescription { get; set; } = string.Empty;
}
