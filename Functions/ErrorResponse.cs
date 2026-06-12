using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Http;

namespace TravelCardFunctionApp.Functions;

public static class ErrorResponse
{
    private readonly ILogger<ErrorResponse> _logger;

    public ErrorResponse(ILogger<ErrorResponse> logger)
    {
        _logger = logger;
    }

    public static async Task<HttpResponseData> CreateAsync(HttpRequestData req, HttpStatusCode statusCode, string message)
    {
        var response = req.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(new { error = new { code = (int)statusCode, message } });
        _logger.LogError("Error encountered: {Message}", message);
        return response;
    }
}
