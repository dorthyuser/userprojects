using System.Text.Json.Serialization;

namespace MulecombineMainLambda;

public sealed class SaErrorResponse
{
    [JsonPropertyName("error")]
    public SaErrorBody? Error { get; set; }
}

public sealed class SaErrorBody
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