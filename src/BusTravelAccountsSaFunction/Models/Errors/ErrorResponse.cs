using System.Text.Json.Serialization;

namespace BusTravelAccountsSaFunction;

public sealed class ErrorResponse
{
    [JsonPropertyName("error")]
    public ErrorBody Error { get; set; } = new();

    public static ErrorResponse Create(int code, string message, string description) =>
        new()
        {
            Error = new ErrorBody
            {
                ErrorCode = code,
                ErrorDateTime = DateTimeOffset.UtcNow,
                ErrorMessage = message,
                ErrorDescription = description
            }
        };
}
