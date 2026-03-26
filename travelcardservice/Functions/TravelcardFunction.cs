using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using travelcardservice.Helpers;
using travelcardservice.Models;

namespace travelcardservice.Functions
{
    public class TravelcardFunction
    {
        private readonly ILogger<TravelcardFunction> _logger;
        private readonly DbHelper _dbHelper;
        private readonly IConfiguration _config;

        public TravelcardFunction(ILogger<TravelcardFunction> logger, DbHelper dbHelper, IConfiguration config)
        {
            _logger = logger;
            _dbHelper = dbHelper;
            _config = config;
        }

        [Function("CreateTravelcard")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "travelcards")] HttpRequestData req)
        {
            _logger.LogInformation("Enter CreateTravelcard");
            try
            {
                // Header validations
                if (!req.Headers.TryGetValues("client_id", out var clientIdValues))
                {
                    _logger.LogWarning("Missing client_id header");
                    return await BadRequest(req, "Missing required header: client_id");
                }

                string clientId = string.Empty;
                foreach (var v in clientIdValues) { clientId = v; break; }
                if (string.IsNullOrWhiteSpace(clientId) || clientId.Length > 128)
                {
                    _logger.LogWarning("Invalid client_id header length");
                    return await BadRequest(req, "Invalid client_id header: must be 1-128 characters");
                }

                string correlationId = string.Empty;
                if (req.Headers.TryGetValues("X-Correlation-Cust-Id", out var corr))
                {
                    foreach (var v in corr) { correlationId = v; break; }
                    if (correlationId.Length > 100)
                    {
                        _logger.LogWarning("Invalid X-Correlation-Cust-Id length");
                        return await BadRequest(req, "Invalid X-Correlation-Cust-Id header: max 100 characters");
                    }
                }

                // Content-Type enforcement if body exists
                if (!req.Headers.TryGetValues("Content-Type", out var contentTypeValues))
                {
                    _logger.LogWarning("Missing Content-Type header");
                    return await BadRequest(req, "Missing Content-Type header");
                }
                bool hasJson = false;
                foreach (var ct in contentTypeValues) { if (ct != null && ct.IndexOf("application/json", StringComparison.OrdinalIgnoreCase) >= 0) { hasJson = true; break; } }
                if (!hasJson)
                {
                    _logger.LogWarning("Unsupported Content-Type");
                    return await BadRequest(req, "Content-Type must be application/json");
                }

                string body;
                using (var sr = new StreamReader(req.Body))
                {
                    body = await sr.ReadToEndAsync();
                }

                if (string.IsNullOrWhiteSpace(body))
                {
                    _logger.LogWarning("Empty request body");
                    return await BadRequest(req, "Request body is required");
                }

                var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
                TravelcardRequest request;
                try
                {
                    request = JsonSerializer.Deserialize<TravelcardRequest>(body, options) ?? new TravelcardRequest();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Invalid JSON payload");
                    return await BadRequest(req, "Invalid JSON payload");
                }

                // Request-level validations
                var now = DateTime.UtcNow;

                if (request.TravelcardRequestedDate >= now)
                {
                    return await ValidationError(req, "travelcardRequestedDate must be in the past");
                }

                // Ensure valid_from is earlier than valid_to
                if (request.TravelcardValidFrom >= request.TravelcardValidTo)
                {
                    return await ValidationError(req, "travelcardValidFrom must be earlier than travelcardValidTo");
                }

                if (request.TravelcardValidTo <= now)
                {
                    return await ValidationError(req, "travelcardValidTo must be in the future");
                }

                if (request.TravelcardType == TravelcardType.SixteenToSeventeen)
                {
                    if (!request.TravelcardUsableTo.HasValue)
                    {
                        return await ValidationError(req, "travelcardUsableTo is required for TravelcardType 'SixteenToSeventeen'");
                    }
                }

                if (request.TravelcardUsableTo.HasValue && request.TravelcardUsableTo.Value <= now)
                {
                    return await ValidationError(req, "travelcardUsableTo must be in the future");
                }

                if (string.IsNullOrWhiteSpace(request.TravelcardNumber) || request.TravelcardNumber.Length < 11 || request.TravelcardNumber.Length > 22)
                {
                    return await ValidationError(req, "travelcardNumber length must be between 11 and 22 characters");
                }

                if (string.IsNullOrWhiteSpace(request.TravelcardTransactionReference) || request.TravelcardTransactionReference.Length != 15)
                {
                    return await ValidationError(req, "travelcardTransactionReference must be exactly 15 characters as per format");
                }

                if (!string.IsNullOrEmpty(request.TravelcardName) && request.TravelcardName.Length > 255)
                {
                    return await ValidationError(req, "travelcardName must be <= 255 characters");
                }

                if (request.Cardholders == null || request.Cardholders.Count < 1 || request.Cardholders.Count > 2)
                {
                    return await ValidationError(req, "cardholders must contain exactly 1 primary and optional 1 secondary (1 or 2 items only)");
                }

                int primaryCount = 0;
                foreach (var ch in request.Cardholders)
                {
                    if (ch.CardholderType == CardholderType.Primary) primaryCount++;
                    if (string.IsNullOrWhiteSpace(ch.CardholderTitle) || ch.CardholderTitle.Length > 15)
                    {
                        return await ValidationError(req, "cardholderTitle is required and must be 1-15 characters");
                    }
                    if (string.IsNullOrWhiteSpace(ch.CardholderForename) || ch.CardholderForename.Length > 100)
                    {
                        return await ValidationError(req, "cardholderForename is required and must be 1-100 characters");
                    }
                    if (string.IsNullOrWhiteSpace(ch.CardholderSurname) || ch.CardholderSurname.Length > 100)
                    {
                        return await ValidationError(req, "cardholderSurname is required and must be 1-100 characters");
                    }
                    if (string.IsNullOrWhiteSpace(ch.CardholderPhotoName) || ch.CardholderPhotoName.Length > 100)
                    {
                        return await ValidationError(req, "cardholderPhotoName is required and must be 1-100 characters");
                    }

                    int imageProvided = 0;
                    if (!string.IsNullOrEmpty(ch.CardholderPhotoRRSKey)) imageProvided++;
                    if (!string.IsNullOrEmpty(ch.CardholderPhotoURL)) imageProvided++;
                    if (!string.IsNullOrEmpty(ch.CardholderPhotoKey)) imageProvided++;
                    if (imageProvided != 1)
                    {
                        return await ValidationError(req, "Each cardholder must provide exactly one of cardholderPhotoRRSKey, cardholderPhotoURL, or cardholderPhotoKey");
                    }
                    if (!string.IsNullOrEmpty(ch.CardholderPhotoRRSKey) && (ch.CardholderPhotoRRSKey.Length < 39 || ch.CardholderPhotoRRSKey.Length > 42))
                    {
                        return await ValidationError(req, "cardholderPhotoRRSKey length must be between 39 and 42 characters");
                    }
                    if (!string.IsNullOrEmpty(ch.CardholderPhotoKey) && (ch.CardholderPhotoKey.Length < 39 || ch.CardholderPhotoKey.Length > 42))
                    {
                        return await ValidationError(req, "cardholderPhotoKey length must be between 39 and 42 characters");
                    }
                    if (!string.IsNullOrEmpty(ch.CardholderPhotoURL) && (ch.CardholderPhotoURL.Length < 20 || ch.CardholderPhotoURL.Length > 2048))
                    {
                        return await ValidationError(req, "cardholderPhotoURL length must be between 20 and 2048 characters");
                    }
                }

                if (primaryCount != 1)
                {
                    return await ValidationError(req, "There must be exactly one Primary cardholder");
                }

                // Business rule: Secondary allowed only for TwoTogether and Family
                if (request.Cardholders.Count == 2)
                {
                    bool hasSecondary = false;
                    foreach (var ch in request.Cardholders) if (ch.CardholderType == CardholderType.Secondary) hasSecondary = true;
                    if (hasSecondary && !(request.TravelcardType == TravelcardType.TwoTogether || request.TravelcardType == TravelcardType.Family))
                    {
                        return await ValidationError(req, "Secondary cardholder is only allowed for Travelcard types: TwoTogether, Family");
                    }
                }

                // Insert into DB
                int dbId = await _dbHelper.InsertTravelcardAsync(request);
                await _dbHelper.InsertCardholdersAsync(dbId, request.Cardholders);

                // Response generation
                var responseObj = new CreateResponse { TravelcardId = Guid.NewGuid().ToString(), Token = GenerateToken(6) };

                var response = req.CreateResponse(HttpStatusCode.Created);
                response.Headers.Add("Content-Type", "application/json");
                var respJson = JsonSerializer.Serialize(responseObj);
                await response.WriteStringAsync(respJson);

                _logger.LogInformation("Exit CreateTravelcard successfully");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in CreateTravelcard");
                return await InternalError(req, ex.Message);
            }
        }

        private static string GenerateToken(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var rnd = new Random();
            var arr = new char[length];
            for (int i = 0; i < length; i++) arr[i] = chars[rnd.Next(chars.Length)];
            return new string(arr);
        }

        private async Task<HttpResponseData> ValidationError(HttpRequestData req, string message)
        {
            var response = req.CreateResponse(HttpStatusCode.BadRequest);
            response.Headers.Add("Content-Type", "application/json");
            var obj = new { errorCode = "VALIDATION_ERROR", message };
            await response.WriteStringAsync(JsonSerializer.Serialize(obj));
            return response;
        }

        private async Task<HttpResponseData> BadRequest(HttpRequestData req, string message)
        {
            var response = req.CreateResponse(HttpStatusCode.BadRequest);
            response.Headers.Add("Content-Type", "application/json");
            var obj = new { errorCode = "BAD_REQUEST", message };
            await response.WriteStringAsync(JsonSerializer.Serialize(obj));
            return response;
        }

        private async Task<HttpResponseData> InternalError(HttpRequestData req, string message)
        {
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            response.Headers.Add("Content-Type", "application/json");
            var obj = new { errorCode = "INTERNAL_ERROR", message };
            await response.WriteStringAsync(JsonSerializer.Serialize(obj));
            return response;
        }
    }
}
