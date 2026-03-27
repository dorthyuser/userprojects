using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using TravelcardFunction.Helpers;
using TravelcardFunction.Models;

namespace TravelcardFunction.Functions
{
    public class CreateTravelcardFunction
    {
        private readonly ILogger _logger;
        private readonly DbHelper _dbHelper;

        public CreateTravelcardFunction(ILoggerFactory loggerFactory, DbHelper dbHelper)
        {
            _logger = loggerFactory.CreateLogger<CreateTravelcardFunction>();
            _dbHelper = dbHelper;
        }

        [Function("CreateTravelcard")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "travelcard")] HttpRequestData req)
        {
            _logger.LogInformation("Enter CreateTravelcardFunction.Run");
            try
            {
                // Header validation
                if (!req.Headers.TryGetValues("client_id", out var clientIds))
                {
                    var resp400 = req.CreateResponse(HttpStatusCode.BadRequest);
                    await resp400.WriteAsJsonAsync(new ErrorResponse { Error = "Missing required header: client_id" });
                    _logger.LogWarning("Missing client_id header");
                    return resp400;
                }

                var clientId = System.Linq.Enumerable.FirstOrDefault(clientIds) ?? string.Empty;
                if (string.IsNullOrWhiteSpace(clientId) || clientId.Length > 128)
                {
                    var resp400 = req.CreateResponse(HttpStatusCode.BadRequest);
                    await resp400.WriteAsJsonAsync(new ErrorResponse { Error = "Invalid client_id header" });
                    _logger.LogWarning("Invalid client_id header");
                    return resp400;
                }

                if (req.Headers.TryGetValues("Content-Type", out var contentTypes))
                {
                    var ct = System.Linq.Enumerable.FirstOrDefault(contentTypes) ?? string.Empty;
                    if (!ct.Contains("application/json", StringComparison.OrdinalIgnoreCase))
                    {
                        var resp415 = req.CreateResponse(HttpStatusCode.UnsupportedMediaType);
                        await resp415.WriteAsJsonAsync(new ErrorResponse { Error = "Content-Type must be application/json" });
                        _logger.LogWarning("Unsupported content type: {ContentType}", ct);
                        return resp415;
                    }
                }

                // Deserialize with enum converter
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                options.Converters.Add(new JsonStringEnumConverter());

                TravelcardRequest? requestBody;
                try
                {
                    requestBody = await JsonSerializer.DeserializeAsync<TravelcardRequest>(req.Body, options);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to deserialize request body");
                    var resp400 = req.CreateResponse(HttpStatusCode.BadRequest);
                    await resp400.WriteAsJsonAsync(new ErrorResponse { Error = "Invalid JSON payload" });
                    return resp400;
                }

                if (requestBody == null)
                {
                    var resp400 = req.CreateResponse(HttpStatusCode.BadRequest);
                    await resp400.WriteAsJsonAsync(new ErrorResponse { Error = "Request body is required" });
                    _logger.LogWarning("Empty request body");
                    return resp400;
                }

                // Validate payload
                var validator = new ValidationService();
                var validationResult = validator.ValidateTravelcardRequest(requestBody);
                if (!validationResult.IsValid)
                {
                    var resp422 = req.CreateResponse((HttpStatusCode)422);
                    await resp422.WriteAsJsonAsync(new ErrorResponse { Error = "Validation failed", Details = validationResult.Errors });
                    _logger.LogWarning("Validation failed: {Errors}", string.Join(";", validationResult.Errors));
                    return resp422;
                }

                // Persist to DB
                var travelcardGuid = Guid.NewGuid();
                string token = TokenGenerator.GenerateToken(6);

                try
                {
                    var dbResult = await _dbHelper.InsertTravelcardAsync(requestBody);
                    // dbResult.TravelcardDbId is int id
                    var resp201 = req.CreateResponse(HttpStatusCode.Created);
                    await resp201.WriteAsJsonAsync(new CreateTravelcardResponse { TravelcardId = travelcardGuid.ToString(), Token = token });
                    _logger.LogInformation("Exit CreateTravelcardFunction.Run success, dbId={Id}", dbResult.TravelcardDbId);
                    return resp201;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Database error while inserting travelcard");
                    var resp500 = req.CreateResponse(HttpStatusCode.InternalServerError);
                    await resp500.WriteAsJsonAsync(new ErrorResponse { Error = "Internal server error" });
                    return resp500;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in CreateTravelcardFunction.Run");
                var resp = req.CreateResponse(HttpStatusCode.InternalServerError);
                await resp.WriteAsJsonAsync(new ErrorResponse { Error = "Unhandled server error" });
                return resp;
            }
        }
    }
}
