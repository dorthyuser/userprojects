using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using TravelcardFunctionApp.Helpers;
using TravelcardFunctionApp.Models;

namespace TravelcardFunctionApp.Functions
{
    public class TravelcardFunction
    {
        private readonly ILogger _logger;
        private readonly ITravelcardService _travelcardService;

        public TravelcardFunction(ILoggerFactory loggerFactory, ITravelcardService travelcardService)
        {
            _logger = loggerFactory.CreateLogger<TravelcardFunction>();
            _travelcardService = travelcardService;
        }

        [Function("TravelcardFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "travelcard")] HttpRequestData req)
        {
            _logger.LogInformation("Entering TravelcardFunction at {Time}", DateTime.UtcNow);
            try
            {
                string requestBody;
                using (var reader = new StreamReader(req.Body))
                {
                    requestBody = await reader.ReadToEndAsync();
                }

                if (string.IsNullOrWhiteSpace(requestBody))
                {
                    _logger.LogWarning("Request body is empty");
                    var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    var error = new ErrorResponse { Error = new ErrorDetail { Message = "Request body is required", Detail = "The POST body must contain JSON." } };
                    await badResponse.WriteAsJsonAsync(error);
                    _logger.LogInformation("Exiting TravelcardFunction with 400 at {Time}", DateTime.UtcNow);
                    return badResponse;
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                TravelcardRequest? travelcardRequest;
                try
                {
                    travelcardRequest = JsonSerializer.Deserialize<TravelcardRequest>(requestBody, options);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Invalid JSON payload");
                    var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    var error = new ErrorResponse { Error = new ErrorDetail { Message = "Invalid JSON", Detail = ex.Message } };
                    await badResponse.WriteAsJsonAsync(error);
                    _logger.LogInformation("Exiting TravelcardFunction with 400 at {Time}", DateTime.UtcNow);
                    return badResponse;
                }

                if (travelcardRequest == null)
                {
                    _logger.LogWarning("Deserialized request is null");
                    var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    var error = new ErrorResponse { Error = new ErrorDetail { Message = "Invalid request", Detail = "Unable to parse request body." } };
                    await badResponse.WriteAsJsonAsync(error);
                    _logger.LogInformation("Exiting TravelcardFunction with 400 at {Time}", DateTime.UtcNow);
                    return badResponse;
                }

                // Basic validation
                if (travelcardRequest.Connection == null || travelcardRequest.Auth == null || travelcardRequest.Auth.OAuth2 == null)
                {
                    _logger.LogWarning("Missing required fields in request");
                    var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    var error = new ErrorResponse { Error = new ErrorDetail { Message = "Validation failed", Detail = "connection and auth.oauth2 are required." } };
                    await badResponse.WriteAsJsonAsync(error);
                    _logger.LogInformation("Exiting TravelcardFunction with 400 at {Time}", DateTime.UtcNow);
                    return badResponse;
                }

                var result = await _travelcardService.PostTravelcardAsync(travelcardRequest);

                var response = req.CreateResponse((System.Net.HttpStatusCode)result.StatusCode);
                await response.WriteAsJsonAsync(result);
                _logger.LogInformation("Exiting TravelcardFunction with {Status} at {Time}", result.StatusCode, DateTime.UtcNow);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in TravelcardFunction");
                var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                var error = new ErrorResponse { Error = new ErrorDetail { Message = "Internal server error", Detail = ex.Message } };
                await response.WriteAsJsonAsync(error);
                _logger.LogInformation("Exiting TravelcardFunction with 500 at {Time}", DateTime.UtcNow);
                return response;
            }
        }
    }
}
