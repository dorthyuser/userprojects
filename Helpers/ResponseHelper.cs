using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Http;
using TravelcardFunctionApp.Models;

namespace TravelcardFunctionApp.Helpers;

public static class ResponseHelper
{
    public static async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, HttpStatusCode statusCode, string error, string details)
    {
        var response = req.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(new ErrorResponse { Error = error, Details = details });
        return response;
    }
}