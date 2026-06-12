using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Http;

namespace azurefunctionaeproject.Helpers;

public static class ErrorResponseHelper
{
    public static async Task<HttpResponseData> CreateAsync(HttpRequestData req, HttpStatusCode statusCode, string code, string message, string? existingAeId = null)
    {
        var response = req.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json");
        object payload = existingAeId is null
            ? new ErrorPayload(code, message)
            : new ErrorPayloadWithExistingAeId(code, message, existingAeId);
        await response.WriteStringAsync(JsonSerializer.Serialize(payload));
        return response;
    }

    private sealed record ErrorPayload(string Code, string Message)
    {
        public string Status { get; init; } = "error";
    }

    private sealed record ErrorPayloadWithExistingAeId(string Code, string Message, string ExistingAeId)
    {
        public string Status { get; init; } = "error";
    }
}
