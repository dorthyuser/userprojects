using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using TravelcardApi.Helpers;
using TravelcardApi.Models;

namespace TravelcardApi.Functions
{
    public class TravelcardFunction
    {
        private readonly ILogger<TravelcardFunction> _logger;
        private readonly IHttpHelper _httpHelper;

        public TravelcardFunction(ILogger<TravelcardFunction> logger, IHttpHelper httpHelper)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpHelper = httpHelper ?? throw new ArgumentNullException(nameof(httpHelper));
        }

        [Function("PostTravelcard")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "travelcard")] HttpRequestData req, FunctionContext context)
        {
            _logger.LogInformation("Entering PostTravelcard function");
            try
            {
                string requestBody;
                using (var reader = new StreamReader(req.Body))
                {
                    requestBody = await reader.ReadToEndAsync();
                }

                if (string.IsNullOrWhiteSpace(requestBody))
                {
                    _logger.LogWarning("Empty request body received");
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    badResponse.Headers.Add("Content-Type", "application/json");
                    await badResponse.WriteStringAsync(JsonSerializer.Serialize(new
                    {
                        error = new { message = "Request body is empty" }
                    }));
                    return badResponse;
                }

                TravelcardRequest? travelcardRequest;
                try
                {
                    travelcardRequest = JsonSerializer.Deserialize<TravelcardRequest>(requestBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (JsonException jex)
                {
                    _logger.LogError(jex, "Failed to deserialize request body");
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    badResponse.Headers.Add("Content-Type", "application/json");
                    await badResponse.WriteStringAsync(JsonSerializer.Serialize(new
                    {
                        error = new { message = "Invalid JSON in request body", details = jex.Message }
                    }));
                    return badResponse;
                }

                if (travelcardRequest == null)
                {
                    _logger.LogWarning("Deserialized request is null");
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    badResponse.Headers.Add("Content-Type", "application/json");
                    await badResponse.WriteStringAsync(JsonSerializer.Serialize(new
                    {
                        error = new { message = "Request body could not be parsed" }
                    }));
                    return badResponse;
                }

                // Validate required nested fields
                if (string.IsNullOrWhiteSpace(travelcardRequest.Connection.Name))
                {
                    _logger.LogWarning("Connection.name is required");
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    badResponse.Headers.Add("Content-Type", "application/json");
                    await badResponse.WriteStringAsync(JsonSerializer.Serialize(new
                    {
                        error = new { message = "connection.name is required" }
                    }));
                    return badResponse;
                }

                if (travelcardRequest.Auth?.OAuth2 == null)
                {
                    _logger.LogWarning("auth.oauth2 is required");
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    badResponse.Headers.Add("Content-Type", "application/json");
                    await badResponse.WriteStringAsync(JsonSerializer.Serialize(new
                    {
                        error = new { message = "auth.oauth2 is required" }
                    }));
                    return badResponse;
                }

                // Resolve environment variable placeholders in oauth2 settings if any start with $
                travelcardRequest.Auth.OAuth2.ResolveEnvironmentPlaceholders();

                // Forward to backend HTTP helper
                var backendResponse = await _httpHelper.PostTravelcardAsync(travelcardRequest, requestBody);

                var clientResponse = req.CreateResponse(backendResponse.IsSuccessStatusCode ? HttpStatusCode.OK : HttpStatusCode.BadGateway);
                clientResponse.Headers.Add("Content-Type", "application/json");

                var backendContent = await backendResponse.Content.ReadAsStringAsync();

                if (backendResponse.IsSuccessStatusCode)
                {
                    await clientResponse.WriteStringAsync(JsonSerializer.Serialize(new
                    {
                        success = true,
                        status = (int)backendResponse.StatusCode,
                        data = JsonSerializer.Deserialize<object>(backendContent) ?? backendContent
                    }));
                    _logger.LogInformation("Exiting PostTravelcard function successfully");
                    return clientResponse;
                }

                _logger.LogError("Backend request failed with status {StatusCode}", backendResponse.StatusCode);
                await clientResponse.WriteStringAsync(JsonSerializer.Serialize(new
                {
                    success = false,
                    status = (int)backendResponse.StatusCode,
                    error = new { message = "Backend error", details = backendContent }
                }));

                _logger.LogInformation("Exiting PostTravelcard function with backend error");
                return clientResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in PostTravelcard");
                var resp = req.CreateResponse(HttpStatusCode.InternalServerError);
                resp.Headers.Add("Content-Type", "application/json");
                await resp.WriteStringAsync(JsonSerializer.Serialize(new
                {
                    error = new { message = "Internal server error", details = ex.Message }
                }));
                return resp;
            }
        }
    }
}
