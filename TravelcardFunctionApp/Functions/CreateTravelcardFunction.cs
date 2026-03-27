using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using TravelcardFunctionApp.Helpers;
using TravelcardFunctionApp.Models;

namespace TravelcardFunctionApp.Functions
{
    public class CreateTravelcardFunction
    {
        private readonly DbHelper _dbHelper;

        public CreateTravelcardFunction(DbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        [Function("CreateTravelcard")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "travelcard")] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("CreateTravelcard");
            logger.LogInformation("Enter CreateTravelcard");

            try
            {
                // Header validations
                if (!req.Headers.TryGetValues("client_id", out var clientIds) || string.IsNullOrWhiteSpace(clientIds.FirstOrDefault()))
                {
                    var badResp = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResp.WriteAsJsonAsync(new { error = "Missing required header: client_id" });
                    logger.LogWarning("Missing client_id header");
                    return badResp;
                }

                // Content-Type header: only required when body present
                var bodyStream = req.Body ?? Stream.Null;
                bool hasBody = false;
                try
                {
                    hasBody = bodyStream != Stream.Null && (bodyStream.CanSeek ? bodyStream.Length > 0 : true);
                }
                catch
                {
                    hasBody = true;
                }

                if (hasBody)
                {
                    if (!req.Headers.TryGetValues("Content-Type", out var contentTypeValues) || string.IsNullOrWhiteSpace(contentTypeValues.FirstOrDefault()))
                    {
                        var badResp = req.CreateResponse(HttpStatusCode.BadRequest);
                        await badResp.WriteAsJsonAsync(new { error = "Missing Content-Type header" });
                        logger.LogWarning("Missing Content-Type header");
                        return badResp;
                    }
                }

                // Simple Authorization: Bearer token must match configured ApiKey
                var envApiKey = Environment.GetEnvironmentVariable("ApiKey");
                if (!req.Headers.TryGetValues("Authorization", out var authValues) || string.IsNullOrWhiteSpace(authValues.FirstOrDefault()))
                {
                    var unauth = req.CreateResponse(HttpStatusCode.Unauthorized);
                    await unauth.WriteAsJsonAsync(new { error = "Missing Authorization header" });
                    logger.LogWarning("Missing Authorization header");
                    return unauth;
                }

                var authHeader = authValues.First();
                var token = authHeader.StartsWith("Bearer ", StringComparison.Ordinal) ? authHeader.Substring(7) : authHeader;
                if (string.IsNullOrEmpty(envApiKey) || !string.Equals(token, envApiKey, StringComparison.Ordinal))
                {
                    var unauth = req.CreateResponse(HttpStatusCode.Unauthorized);
                    await unauth.WriteAsJsonAsync(new { error = "Invalid API token" });
                    logger.LogWarning("Invalid API token provided");
                    return unauth;
                }

                // Read body
                using var sr = new StreamReader(bodyStream);
                var body = await sr.ReadToEndAsync();
                if (string.IsNullOrWhiteSpace(body))
                {
                    var badResp = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResp.WriteAsJsonAsync(new { error = "Empty request body" });
                    logger.LogWarning("Empty request body");
                    return badResp;
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() }
                };

                TravelcardCreateRequest? request;
                try
                {
                    request = JsonSerializer.Deserialize<TravelcardCreateRequest>(body, options);
                }
                catch (Exception ex)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteAsJsonAsync(new { error = "Invalid JSON payload", detail = ex.Message });
                    logger.LogError(ex, "Failed to deserialize request body");
                    return bad;
                }

                if (request == null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteAsJsonAsync(new { error = "Invalid request payload" });
                    logger.LogWarning("Deserialized request was null");
                    return bad;
                }

                // Validate request
                var validator = new RequestValidator();
                var validationErrors = validator.Validate(request);
                if (validationErrors.Any())
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteAsJsonAsync(new { errors = validationErrors });
                    logger.LogWarning("Validation failed: {errors}", string.Join(";", validationErrors));
                    return bad;
                }

                // Insert into DB
                try
                {
                    var (internalId, tokenOut) = await _dbHelper.InsertTravelcardAsync(request);

                    // Response contains travelcardId (GUID per spec) and token
                    var travelcardGuid = Guid.NewGuid().ToString();
                    var resp = req.CreateResponse(HttpStatusCode.Created);
                    await resp.WriteAsJsonAsync(new { travelcardId = travelcardGuid, token = tokenOut });
                    logger.LogInformation("Exit CreateTravelcard - created travelcard id {id} (db id {dbid})", travelcardGuid, internalId);
                    return resp;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error inserting travelcard into database");
                    var err = req.CreateResponse(HttpStatusCode.InternalServerError);
                    await err.WriteAsJsonAsync(new { error = "Internal server error" });
                    return err;
                }
            }
            catch (Exception ex)
            {
                var logger2 = context.GetLogger("CreateTravelcard");
                logger2.LogError(ex, "Unhandled error in CreateTravelcard");
                var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                await response.WriteAsJsonAsync(new { error = "Unhandled error" });
                return response;
            }
        }
    }
}
