using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Http;
using travelcard_function_app.Models;

namespace travelcard_function_app.Helpers;

public static class ResponseHelper
{
    public static async Task<HttpResponseData> CreateErrorResponseAsync(HttpRequestData req, HttpStatusCode statusCode, string message, string code)
    {
        var response = req.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(new ErrorResponse
        {
            Error = new ErrorDetail
            {
                Code = code,
                Message = message
            }
        });
        return response;
    }
}
