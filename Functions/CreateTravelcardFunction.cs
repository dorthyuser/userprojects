using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Models;
using Helpers;

namespace Functions
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
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        [Function("CreateTravelcardFunction")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "travelcard")] HttpRequestData req)
        {
            string correlationId = req.Headers.TryGetValues("X-Correlation-Cust-Id", out var corrValues) ? string.Join(",", corrValues) : string.Empty;
            _logger.LogInformation("[ENTRY] CreateTravelcardFunction CorrelationId={CorrelationId}", correlationId);

            try
            {
                if (!req.Headers.TryGetValues("client_id", out var clientIdValues))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Missing required header", "client_id header is required.");
                }

                string clientId = string.Join(",", clientIdValues);
                if (string.IsNullOrWhiteSpace(clientId) || clientId.Length > 128)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid header", "client_id must be between 1 and 128 characters.");
                }

                if (!req.Headers.TryGetValues("Content-Type", out var contentTypeValues) || !string.Join(",", contentTypeValues).Contains("application/json", StringComparison.OrdinalIgnoreCase))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid header", "Content-Type must be application/json.");
                }

                string body = await new System.IO.StreamReader(req.Body).ReadToEndAsync();
                if (string.IsNullOrWhiteSpace(body))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request", "Request body is required.");
                }

                CreateTravelcardRequest? request;
                try
                {
                    request = JsonSerializer.Deserialize<CreateTravelcardRequest>(body, _jsonOptions);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[ERROR] JSON deserialization failed");
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid JSON", ex.Message);
                }

                if (request is null)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request", "Request body could not be parsed.");
                }

                ValidationResult validation = TravelcardValidator.Validate(request);
                if (!validation.IsValid)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Validation failed", validation.ErrorMessage);
                }

                TravelcardResponse response = await _dbHelper.CreateTravelcardAsync(request);
                HttpResponseData okResponse = req.CreateResponse(HttpStatusCode.OK);
                await okResponse.WriteAsJsonAsync(response);
                _logger.LogInformation("[EXIT] CreateTravelcardFunction Success TravelcardId={TravelcardId}", response.TravelcardId);
                return okResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ERROR] Unexpected failure in CreateTravelcardFunction");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Internal server error", ex.Message);
            }
        }

        private static async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, HttpStatusCode statusCode, string error, string details)
        {
            HttpResponseData response = req.CreateResponse(statusCode);
            await response.WriteAsJsonAsync(new ErrorResponse(error, details));
            return response;
        }
    }
}