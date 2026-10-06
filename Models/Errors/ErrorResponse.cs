using System;
using System.Text.Json.Serialization;

namespace BusTravelAccountsEaMainFunction;

public sealed class ErrorResponse
{
    [JsonPropertyName("error")]
    public ErrorDetails Error { get; set; } = new();
}

public sealed class ErrorDetails
{
    [JsonPropertyName("errorCode")]
    public int ErrorCode { get; set; }

    [JsonPropertyName("errorDateTime")]
    public DateTimeOffset ErrorDateTime { get; set; }

    [JsonPropertyName("errorMessage")]
    public string ErrorMessage { get; set; } = string.Empty;

    [JsonPropertyName("errorDescription")]
    public object? ErrorDescription { get; set; }
}