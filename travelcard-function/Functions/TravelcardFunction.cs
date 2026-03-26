using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using travelcard_function.Models;
using travelcard_function.Helpers;

namespace travelcard_function.Functions
{
    public class TravelcardFunction
    {
        private readonly IDbHelper _dbHelper;
        private readonly ILogger _logger;

        public TravelcardFunction(IDbHelper dbHelper, ILoggerFactory loggerFactory)
        {
            _dbHelper = dbHelper ?? throw new ArgumentNullException(nameof(dbHelper));
            _logger = loggerFactory.CreateLogger<TravelcardFunction>();
        }

        [Function("CreateTravelcard")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "travelcard")] HttpRequestData req, FunctionContext context)
        {
            _logger.LogInformation("Enter CreateTravelcard");
            var response = req.CreateResponse();
            try
            {
                // Header validation
                if (!req.Headers.TryGetValues("client_id", out var clientIds))
                {
                    _logger.LogWarning("Missing client_id header");
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteAsJsonAsync(new ErrorResponse("Missing required header: client_id"));
                    _logger.LogInformation("Exit CreateTravelcard");
                    return response;
                }

                var clientId = string.Empty;
                foreach (var v in clientIds) { clientId = v; break; }
                if (string.IsNullOrWhiteSpace(clientId) || clientId.Length > 128)
                {
                    _logger.LogWarning("Invalid client_id header");
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteAsJsonAsync(new ErrorResponse("Invalid client_id header"));
                    _logger.LogInformation("Exit CreateTravelcard");
                    return response;
                }

                if (req.Headers.TryGetValues("Content-Type", out var contentTypes))
                {
                    var ct = string.Empty;
                    foreach (var v in contentTypes) { ct = v; break; }
                    if (!ct.Contains("application/json"))
                    {
                        response.StatusCode = HttpStatusCode.UnsupportedMediaType;
                        await response.WriteAsJsonAsync(new ErrorResponse("Content-Type must be application/json"));
                        _logger.LogInformation("Exit CreateTravelcard");
                        return response;
                    }
                }

                using var reader = new StreamReader(req.Body);
                var body = await reader.ReadToEndAsync();
                if (string.IsNullOrWhiteSpace(body))
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteAsJsonAsync(new ErrorResponse("Request body is required"));
                    _logger.LogInformation("Exit CreateTravelcard");
                    return response;
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                var request = JsonSerializer.Deserialize<CreateTravelcardRequest>(body, options);
                if (request == null)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteAsJsonAsync(new ErrorResponse("Invalid JSON payload"));
                    _logger.LogInformation("Exit CreateTravelcard");
                    return response;
                }

                // Validate business rules
                var validator = new TravelcardValidator();
                var validation = validator.Validate(request);
                if (!validation.IsValid)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteAsJsonAsync(new ErrorResponse(validation.ErrorMessage));
                    _logger.LogInformation("Exit CreateTravelcard");
                    return response;
                }

                // Persist to DB
                var result = await _dbHelper.CreateTravelcardAsync(request);
                if (!result.Success)
                {
                    _logger.LogError("DB error: {0}", result.ErrorMessage);
                    response.StatusCode = HttpStatusCode.InternalServerError;
                    await response.WriteAsJsonAsync(new ErrorResponse("Internal server error"));
                    _logger.LogInformation("Exit CreateTravelcard");
                    return response;
                }

                var respObj = new CreateTravelcardResponse { travelcardId = result.TravelcardGuid, token = result.Token };
                response.StatusCode = HttpStatusCode.Created;
                await response.WriteAsJsonAsync(respObj);
                _logger.LogInformation("Exit CreateTravelcard");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in CreateTravelcard");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteAsJsonAsync(new ErrorResponse("Unhandled error"));
                _logger.LogInformation("Exit CreateTravelcard");
                return response;
            }
        }
    }
}
