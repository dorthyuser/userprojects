using System;
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
    public class CreateTravelcardFunction
    {
        private readonly PostgresHelper _db;

        public CreateTravelcardFunction(PostgresHelper db)
        {
            _db = db;
        }

        [Function("CreateTravelcard")]
        public async Task<HttpResponseData> Run([
            HttpTrigger(AuthorizationLevel.Function, "post", Route = "travelcard")]
            HttpRequestData req,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("CreateTravelcard");
            logger.LogInformation("Enter CreateTravelcard");

            try
            {
                // Header validations
                if (!req.Headers.TryGetValues("client_id", out var clientIds))
                {
                    logger.LogWarning("Missing client_id header");
                    var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                    await resp.WriteAsJsonAsync(new ErrorResponse { Error = "Missing required header: client_id" });
                    logger.LogInformation("Exit CreateTravelcard with 400");
                    return resp;
                }

                string clientId = string.Empty;
                foreach (var v in clientIds) { clientId = v; break; }
                if (string.IsNullOrWhiteSpace(clientId) || clientId.Length > 128)
                {
                    logger.LogWarning("Invalid client_id header");
                    var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                    await resp.WriteAsJsonAsync(new ErrorResponse { Error = "Invalid client_id header" });
                    logger.LogInformation("Exit CreateTravelcard with 400");
                    return resp;
                }

                if (req.Headers.TryGetValues("Content-Type", out var contentTypes))
                {
                    bool hasJson = false;
                    foreach (var ct in contentTypes) { if (ct.Contains("application/json", StringComparison.OrdinalIgnoreCase)) { hasJson = true; break; } }
                    if (!hasJson)
                    {
                        logger.LogWarning("Invalid Content-Type header");
                        var resp = req.CreateResponse(HttpStatusCode.UnsupportedMediaType);
                        await resp.WriteAsJsonAsync(new ErrorResponse { Error = "Content-Type must be application/json" });
                        logger.LogInformation("Exit CreateTravelcard with 415");
                        return resp;
                    }
                }

                req.Headers.TryGetValues("X-Correlation-Cust-Id", out var correlationValues);
                string correlationId = string.Empty;
                if (correlationValues != null)
                {
                    foreach (var c in correlationValues) { correlationId = c; break; }
                }

                // Parse body
                string body = await new StreamReader(req.Body).ReadToEndAsync();
                TravelcardCreateRequest request;
                try
                {
                    var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    request = JsonSerializer.Deserialize<TravelcardCreateRequest>(body, opts) ?? throw new Exception("Empty body");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Bad request body");
                    var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                    await resp.WriteAsJsonAsync(new ErrorResponse { Error = "Invalid JSON body" });
                    logger.LogInformation("Exit CreateTravelcard with 400");
                    return resp;
                }

                // Business validations
                var validation = RequestValidator.Validate(request);
                if (!validation.IsValid)
                {
                    logger.LogWarning("Validation failed: {0}", validation.Error);
                    var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                    await resp.WriteAsJsonAsync(new ErrorResponse { Error = validation.Error });
                    logger.LogInformation("Exit CreateTravelcard with 400");
                    return resp;
                }

                // Database operations
                logger.LogInformation("Inserting travelcard into DB");
                var travelcardGuid = Guid.NewGuid();
                string token = TokenGenerator.GenerateToken(6);

                try
                {
                    int travelcardDbId = await _db.InsertTravelcardAsync(request, travelcardGuid, executionContext);
                    await _db.InsertCardholdersAsync(travelcardDbId, request.Cardholders, executionContext);
                }
                catch (Exception dbEx)
                {
                    logger.LogError(dbEx, "Database error");
                    var resp = req.CreateResponse(HttpStatusCode.InternalServerError);
                    await resp.WriteAsJsonAsync(new ErrorResponse { Error = "Internal server error" });
                    logger.LogInformation("Exit CreateTravelcard with 500");
                    return resp;
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new TravelcardCreateResponse { TravelcardId = travelcardGuid.ToString(), Token = token });
                logger.LogInformation("Exit CreateTravelcard with 200");
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception");
                var resp = req.CreateResponse(HttpStatusCode.InternalServerError);
                await resp.WriteAsJsonAsync(new ErrorResponse { Error = "Unhandled error" });
                return resp;
            }
        }
    }
}
