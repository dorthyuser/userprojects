using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using TravelcardFunction.Helpers;
using TravelcardFunction.Models;

namespace TravelcardFunction.Functions
{
    public class TravelcardFunction
    {
        private readonly PostgresDb _db;

        public TravelcardFunction(PostgresDb db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        [Function("CreateTravelcard")]
        public async Task<HttpResponseData> CreateTravelcard([HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "travelcard")] HttpRequestData req, FunctionContext context)
        {
            var logger = context.GetLogger("CreateTravelcard");
            logger.LogInformation("Entering CreateTravelcard");

            try
            {
                // Header validation
                if (!req.Headers.TryGetValues("client_id", out var clientIdVals) || string.IsNullOrWhiteSpace(clientIdVals.FirstOrDefault()))
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteAsJsonAsync(new ErrorResponse { Error = "Missing required header: client_id" });
                    logger.LogWarning("Missing required header client_id");
                    return bad;
                }

                var clientId = clientIdVals.First();
                if (clientId.Length < 1 || clientId.Length > 128)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteAsJsonAsync(new ErrorResponse { Error = "Invalid client_id length" });
                    logger.LogWarning("Invalid client_id length");
                    return bad;
                }

                if (req.Headers.TryGetValues("X-Correlation-Cust-Id", out var corrVals))
                {
                    var corr = corrVals.FirstOrDefault();
                    if (corr != null && corr.Length > 100)
                    {
                        var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                        await bad.WriteAsJsonAsync(new ErrorResponse { Error = "X-Correlation-Cust-Id exceeds 100 characters" });
                        logger.LogWarning("X-Correlation-Cust-Id too long");
                        return bad;
                    }
                }

                // Auth for backend integrations: expect x-api-key header
                if (!req.Headers.TryGetValues("x-api-key", out var apiKeyVals) || string.IsNullOrWhiteSpace(apiKeyVals.FirstOrDefault()))
                {
                    var unauthorized = req.CreateResponse(HttpStatusCode.Unauthorized);
                    await unauthorized.WriteAsJsonAsync(new ErrorResponse { Error = "Missing x-api-key header" });
                    logger.LogWarning("Missing x-api-key header");
                    return unauthorized;
                }

                var providedKey = apiKeyVals.First();
                var config = context.InstanceServices.GetService(typeof(Microsoft.Extensions.Configuration.IConfiguration)) as Microsoft.Extensions.Configuration.IConfiguration;
                var expectedKey = config?.GetValue<string>("BackendApiKey");
                if (string.IsNullOrEmpty(expectedKey) || providedKey != expectedKey)
                {
                    var unauthorized = req.CreateResponse(HttpStatusCode.Unauthorized);
                    await unauthorized.WriteAsJsonAsync(new ErrorResponse { Error = "Invalid x-api-key" });
                    logger.LogWarning("Invalid x-api-key provided");
                    return unauthorized;
                }

                // Content-Type
                if (!req.Headers.TryGetValues("Content-Type", out var contentTypeVals) || !contentTypeVals.First().Contains("application/json"))
                {
                    var bad = req.CreateResponse(HttpStatusCode.UnsupportedMediaType);
                    await bad.WriteAsJsonAsync(new ErrorResponse { Error = "Content-Type must be application/json" });
                    logger.LogWarning("Unsupported Content-Type");
                    return bad;
                }

                // Read body
                TravelcardRequest request;
                try
                {
                    request = await JsonSerializer.DeserializeAsync<TravelcardRequest>(req.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new TravelcardRequest();
                }
                catch (Exception ex)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteAsJsonAsync(new ErrorResponse { Error = "Invalid JSON body", Details = ex.Message });
                    logger.LogError(ex, "Failed to deserialize request body");
                    return bad;
                }

                // Validate payload
                var validator = new RequestValidator();
                var validation = validator.Validate(request);
                if (!validation.IsValid)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteAsJsonAsync(new ErrorResponse { Error = "Validation failed", Details = validation.Error });
                    logger.LogWarning("Validation failed: {0}", validation.Error);
                    return bad;
                }

                // Insert into database
                var insertedId = await _db.InsertTravelcardAsync(request, logger);

                // Create response per spec
                var responseBody = new TravelcardResponse
                {
                    TravelcardId = Guid.NewGuid().ToString(),
                    Token = TokenGenerator.Generate(6)
                };

                var ok = req.CreateResponse(HttpStatusCode.Created);
                await ok.WriteAsJsonAsync(responseBody);
                logger.LogInformation("Exiting CreateTravelcard - created id {Id}", insertedId);
                return ok;
            }
            catch (Exception ex)
            {
                var logger2 = context.GetLogger("CreateTravelcard");
                logger2.LogError(ex, "Unhandled exception in CreateTravelcard");
                var resp = req.CreateResponse(HttpStatusCode.InternalServerError);
                await resp.WriteAsJsonAsync(new ErrorResponse { Error = "Internal server error", Details = ex.Message });
                return resp;
            }
        }
    }
}
