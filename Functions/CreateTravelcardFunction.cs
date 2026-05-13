using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using travelcardlambdachsarp549.Helpers;
using travelcardlambdachsarp549.Models;

namespace travelcardlambdachsarp549.Functions
{
    public class CreateTravelcardFunction
    {
        private readonly DbHelper _dbHelper;
        private readonly ILogger<CreateTravelcardFunction> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public CreateTravelcardFunction(DbHelper dbHelper, ILogger<CreateTravelcardFunction> logger)
        {
            _dbHelper = dbHelper;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };
        }

        [Function("CreateTravelcardFunction")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "travelcards")] HttpRequestData req)
        {
            string? correlationId = req.Headers.TryGetValues("X-Correlation-Cust-Id", out var values) ? string.Join(",", values) : null;
            using var scope = _logger.BeginScope("CorrelationId={CorrelationId}", correlationId ?? string.Empty);
            _logger.LogInformation("Entering CreateTravelcardFunction");

            try
            {
                if (!req.Headers.TryGetValues("client_id", out var clientValues))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Missing client_id header", "client_id header is required.");
                }

                string clientId = string.Join(",", clientValues);
                if (string.IsNullOrWhiteSpace(clientId) || clientId.Length > 128)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid client_id header", "client_id must be 1 to 128 characters.");
                }

                if (!req.Headers.TryGetValues("Content-Type", out var contentTypeValues) || !string.Join(",", contentTypeValues).Contains("application/json", StringComparison.OrdinalIgnoreCase))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid Content-Type", "Content-Type must be application/json.");
                }

                if (correlationId != null && correlationId.Length > 100)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid X-Correlation-Cust-Id header", "X-Correlation-Cust-Id must be 100 characters or less.");
                }

                string body;
                using (var reader = new StreamReader(req.Body))
                {
                    body = await reader.ReadToEndAsync();
                }

                if (string.IsNullOrWhiteSpace(body))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Request body is required", "A JSON body is required.");
                }

                CreateTravelcardRequest? request;
                try
                {
                    request = JsonSerializer.Deserialize<CreateTravelcardRequest>(body, _jsonOptions);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to deserialize request body");
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid JSON", ex.Message);
                }

                if (request == null)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request body", "Request body could not be parsed.");
                }

                var validationError = RequestValidator.Validate(request);
                if (!string.IsNullOrEmpty(validationError))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Validation failed", validationError);
                }

                CreateTravelcardResult result = await _dbHelper.CreateTravelcardAsync(request);

                HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new CreateTravelcardResponse
                {
                    TravelcardId = result.TravelcardId,
                    Token = result.Token
                });

                _logger.LogInformation("Exiting CreateTravelcardFunction successfully");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in CreateTravelcardFunction");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Internal server error", ex.Message);
            }
        }

        private static async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, HttpStatusCode statusCode, string error, string details)
        {
            HttpResponseData response = req.CreateResponse(statusCode);
            await response.WriteAsJsonAsync(new ErrorResponse { Error = error, Details = details });
            return response;
        }
    }
}