using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using TravelcardFunction.Models;
using TravelcardFunction.Helpers;

namespace TravelcardFunction.Functions
{
    public class TravelcardFunction
    {
        private readonly IDbHelper _dbHelper;
        private readonly ILogger _logger;

        public TravelcardFunction(IDbHelper dbHelper, ILoggerFactory loggerFactory)
        {
            _dbHelper = dbHelper;
            _logger = loggerFactory.CreateLogger<TravelcardFunction>();
        }

        [Function("CreateTravelcard")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "travelcard")] HttpRequestData req)
        {
            _logger.LogInformation("Enter CreateTravelcard");
            try
            {
                // Header validations
                if (!req.Headers.TryGetValues("client_id", out var clientIds))
                {
                    _logger.LogWarning("Missing client_id header");
                    return await BadRequest(req, "Missing required header: client_id");
                }

                var clientId = System.Linq.Enumerable.FirstOrDefault(clientIds);
                if (string.IsNullOrWhiteSpace(clientId) || clientId.Length > 128)
                {
                    _logger.LogWarning("Invalid client_id header");
                    return await BadRequest(req, "Invalid client_id header");
                }

                if (req.Headers.TryGetValues("Content-Type", out var contentTypes))
                {
                    var ct = System.Linq.Enumerable.FirstOrDefault(contentTypes);
                    if (!string.Equals(ct, "application/json", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning("Unsupported Content-Type");
                        return await BadRequest(req, "Content-Type must be application/json");
                    }
                }

                string correlationId = null;
                if (req.Headers.TryGetValues("X-Correlation-Cust-Id", out var correl))
                {
                    correlationId = System.Linq.Enumerable.FirstOrDefault(correl);
                    if (correlationId != null && correlationId.Length > 100)
                    {
                        _logger.LogWarning("X-Correlation-Cust-Id too long");
                        return await BadRequest(req, "X-Correlation-Cust-Id exceeds 100 characters");
                    }
                }

                using var reader = new StreamReader(req.Body);
                var body = await reader.ReadToEndAsync();
                if (string.IsNullOrWhiteSpace(body))
                {
                    _logger.LogWarning("Empty body");
                    return await BadRequest(req, "Request body is required");
                }

                var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
                options.Converters.Add(new JsonStringEnumConverter());
                var request = JsonSerializer.Deserialize<CreateTravelcardRequest>(body, options);
                if (request == null)
                {
                    _logger.LogWarning("Invalid JSON");
                    return await BadRequest(req, "Invalid JSON payload");
                }

                var validationErrors = RequestValidator.Validate(request);
                if (validationErrors.Count > 0)
                {
                    _logger.LogWarning("Validation failed: {errors}", string.Join(";", validationErrors));
                    return await ValidationError(req, validationErrors);
                }

                // Business specific secondary allowed check
                if (!BusinessRules.SecondaryAllowed(request.TravelcardType, request.Cardholders))
                {
                    _logger.LogWarning("Secondary cardholder not allowed for type {type}", request.TravelcardType);
                    return await BadRequest(req, "Secondary cardholder not allowed for this Travelcard type");
                }

                var result = await _dbHelper.CreateTravelcardAsync(request, clientId, correlationId);

                var response = req.CreateResponse(HttpStatusCode.Created);
                response.Headers.Add("Content-Type", "application/json");
                await response.WriteStringAsync(JsonSerializer.Serialize(new CreateTravelcardResponse
                {
                    TravelcardId = result.TravelcardGuid,
                    Token = result.Token
                }, options));

                _logger.LogInformation("Exit CreateTravelcard success");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in CreateTravelcard");
                return await InternalError(req, "An unexpected error occurred");
            }
        }

        private static async Task<HttpResponseData> BadRequest(HttpRequestData req, string message)
        {
            var response = req.CreateResponse(HttpStatusCode.BadRequest);
            response.Headers.Add("Content-Type", "application/json");
            var payload = JsonSerializer.Serialize(new { error = message });
            await response.WriteStringAsync(payload);
            return response;
        }

        private static async Task<HttpResponseData> ValidationError(HttpRequestData req, List<string> errors)
        {
            var response = req.CreateResponse(HttpStatusCode.UnprocessableEntity);
            response.Headers.Add("Content-Type", "application/json");
            var payload = JsonSerializer.Serialize(new { error = "validation_failed", details = errors });
            await response.WriteStringAsync(payload);
            return response;
        }

        private static async Task<HttpResponseData> InternalError(HttpRequestData req, string message)
        {
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            response.Headers.Add("Content-Type", "application/json");
            var payload = JsonSerializer.Serialize(new { error = message });
            await response.WriteStringAsync(payload);
            return response;
        }
    }
}
