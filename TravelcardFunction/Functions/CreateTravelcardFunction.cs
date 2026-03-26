using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TravelcardFunction.Models;
using TravelcardFunction.Helpers;
using System.Collections.Generic;

namespace TravelcardFunction.Functions
{
    public class CreateTravelcardFunction
    {
        private readonly DbHelper _dbHelper;
        private readonly ILogger _logger;

        public CreateTravelcardFunction(DbHelper dbHelper, ILogger<CreateTravelcardFunction> logger)
        {
            _dbHelper = dbHelper;
            _logger = logger;
        }

        [Function("CreateTravelcard")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "travelcard")] HttpRequestData req, FunctionContext context)
        {
            _logger.LogInformation("Entering CreateTravelcard function");
            try
            {
                // Header validation
                if (!req.Headers.TryGetValues("client_id", out var clientIdValues))
                {
                    var resp = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    await resp.WriteAsJsonAsync(new ErrorResponse { Error = "MissingRequiredHeader", Message = "client_id header is required" });
                    _logger.LogWarning("Missing client_id header");
                    return resp;
                }

                var clientId = string.Empty;
                foreach (var v in clientIdValues) { clientId = v; break; }
                if (string.IsNullOrWhiteSpace(clientId) || clientId.Length > 128)
                {
                    var resp = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    await resp.WriteAsJsonAsync(new ErrorResponse { Error = "InvalidHeader", Message = "client_id must be between 1 and 128 characters" });
                    _logger.LogWarning("Invalid client_id header");
                    return resp;
                }

                if (req.Headers.TryGetValues("Content-Type", out var contentTypes))
                {
                    var hasJson = false;
                    foreach (var ct in contentTypes)
                    {
                        if (ct?.StartsWith("application/json", StringComparison.OrdinalIgnoreCase) == true) { hasJson = true; break; }
                    }
                    if (!hasJson)
                    {
                        var resp = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                        await resp.WriteAsJsonAsync(new ErrorResponse { Error = "InvalidContentType", Message = "Content-Type must be application/json" });
                        _logger.LogWarning("Invalid Content-Type header");
                        return resp;
                    }
                }

                // Read body
                using var sr = new StreamReader(req.Body);
                var body = await sr.ReadToEndAsync();
                if (string.IsNullOrWhiteSpace(body))
                {
                    var resp = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    await resp.WriteAsJsonAsync(new ErrorResponse { Error = "EmptyBody", Message = "Request body is required" });
                    _logger.LogWarning("Empty request body");
                    return resp;
                }

                TravelcardCreateRequest? createRequest = null;
                try
                {
                    createRequest = JsonSerializer.Deserialize<TravelcardCreateRequest>(body, JsonSerializerOptionsProvider.Options);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to deserialize request body");
                    var resp = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    await resp.WriteAsJsonAsync(new ErrorResponse { Error = "InvalidJson", Message = "Request body is not valid JSON" });
                    return resp;
                }

                if (createRequest == null)
                {
                    var resp = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    await resp.WriteAsJsonAsync(new ErrorResponse { Error = "InvalidPayload", Message = "Request body could not be parsed" });
                    _logger.LogWarning("Parsed request body is null");
                    return resp;
                }

                // Validate business rules
                var validation = ValidationHelper.ValidateCreateRequest(createRequest);
                if (!validation.IsValid)
                {
                    var resp = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    await resp.WriteAsJsonAsync(new ErrorResponse { Error = "ValidationError", Message = validation.ErrorMessage });
                    _logger.LogWarning("Validation failed: {message}", validation.ErrorMessage);
                    return resp;
                }

                // Insert into DB
                var travelcardEntity = new TravelcardEntity
                {
                    TravelcardType = createRequest.TravelcardType,
                    TravelcardValidFrom = createRequest.TravelcardValidFrom,
                    TravelcardValidTo = createRequest.TravelcardValidTo,
                    TravelcardName = createRequest.TravelcardName,
                    TravelcardNumber = createRequest.TravelcardNumber,
                    TravelcardRequestedDate = createRequest.TravelcardRequestedDate,
                    TravelcardTransactionReference = createRequest.TravelcardTransactionReference,
                    TravelcardUsableTo = createRequest.TravelcardUsableTo
                };

                int dbId = await _dbHelper.InsertTravelcardAsync(travelcardEntity, createRequest.Cardholders);

                // Generate response values
                var travelcardId = Guid.NewGuid().ToString();
                var token = TokenGenerator.GenerateToken(6);

                var successResp = req.CreateResponse(System.Net.HttpStatusCode.Created);
                await successResp.WriteAsJsonAsync(new { travelcardId = travelcardId, token = token });

                _logger.LogInformation("Exiting CreateTravelcard function successfully: DbId={dbId}", dbId);
                return successResp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in CreateTravelcard");
                var resp = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await resp.WriteAsJsonAsync(new ErrorResponse { Error = "InternalError", Message = "An unexpected error occurred" });
                return resp;
            }
        }
    }
}
