using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using life_time_calculator.Models;
using Microsoft.Azure.Functions.Worker.Http;

namespace life_time_calculator.Helpers;

public static class ErrorResponseHelper
{
    public static async Task<HttpResponseData> CreateAsync(HttpRequestData req, HttpStatusCode statusCode, string message)
    {
        HttpResponseData response = req.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");

        ErrorResponse error = new ErrorResponse
        {
            Error = new ErrorDetails
            {
                Code = ((int)statusCode).ToString(),
                Message = message
            }
        };

        await response.WriteStringAsync(JsonSerializer.Serialize(error, JsonOptionsProvider.Options));
        return response;
    }
}
