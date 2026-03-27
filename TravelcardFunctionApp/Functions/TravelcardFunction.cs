using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using TravelcardFunctionApp.Helpers;
using TravelcardFunctionApp.Models;

namespace TravelcardFunctionApp.Functions
{
    public class TravelcardFunction
    {
        private readonly IDbHelper _dbHelper;
        private readonly ValidationHelper _validationHelper;
        private readonly TokenGenerator _tokenGenerator;
        private readonly ILogger _logger;
        private readonly string _allowedClientId;

        public TravelcardFunction(IDbHelper dbHelper, ValidationHelper validationHelper, TokenGenerator tokenGenerator, ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            _dbHelper = dbHelper;
            _validationHelper = validationHelper;
            _tokenGenerator = tokenGenerator;
            _logger = loggerFactory.CreateLogger<TravelcardFunction>();
            _allowedClientId = configuration["AllowedClientId"] ?? string.Empty;
        }

        [Function("CreateTravelcard")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "travelcard")] HttpRequestData req)
        {
            _logger.LogInformation("Enter CreateTravelcard");
            try
            {
                if (!req.Headers.TryGetValues("client_id", out var clientIds) || string.IsNullOrWhiteSpace(clientIds.FirstOrDefault()))
                {
                    var r400 = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    await r400.WriteAsJsonAsync(new ErrorResponse { Error = "MissingRequiredHeader", Message = "client_id header is required" });
                    _logger.LogWarning("Missing client_id header");
                    return r400;
                }

                var clientId = clientIds.First();
                if (!string.IsNullOrEmpty(_allowedClientId) && !string.Equals(clientId, _allowedClientId, StringComparison.Ordinal))
                {
                    var r401 = req.CreateResponse(System.Net.HttpStatusCode.Unauthorized);
                    await r401.WriteAsJsonAsync(new ErrorResponse { Error = "UnauthorizedClient", Message = "client_id not allowed" });
                    _logger.LogWarning("client_id not allowed: {ClientId}", clientId);
                    return r401;
                }

                if (req.Body == null || !req.Headers.TryGetValues("Content-Type", out var ctVals) || ctVals.All(v => !v.Contains("application/json", StringComparison.OrdinalIgnoreCase)))
                {
                    var r415 = req.CreateResponse(System.Net.HttpStatusCode.UnsupportedMediaType);
                    await r415.WriteAsJsonAsync(new ErrorResponse { Error = "InvalidContentType", Message = "Content-Type must be application/json" });
                    _logger.LogWarning("Invalid content type");
                    return r415;
                }

                using var sr = new StreamReader(req.Body);
                var body = await sr.ReadToEndAsync();
                if (string.IsNullOrWhiteSpace(body))
                {
                    var r400 = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    await r400.WriteAsJsonAsync(new ErrorResponse { Error = "EmptyBody", Message = "Request body is empty" });
                    _logger.LogWarning("Empty request body");
                    return r400;
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

                TravelcardRequest? request;
                try
                {
                    request = JsonSerializer.Deserialize<TravelcardRequest>(body, options);
                }
                catch (Exception ex)
                {
                    var r400 = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    await r400.WriteAsJsonAsync(new ErrorResponse { Error = "InvalidJson", Message = ex.Message });
                    _logger.LogError(ex, "Invalid JSON payload");
                    return r400;
                }

                if (request == null)
                {
                    var r400 = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    await r400.WriteAsJsonAsync(new ErrorResponse { Error = "InvalidPayload", Message = "Payload could not be deserialized" });
                    _logger.LogWarning("Deserialized request is null");
                    return r400;
                }

                var validationErrors = _validationHelper.Validate(request);
                if (validationErrors.Any())
                {
                    var r422 = req.CreateResponse((System.Net.HttpStatusCode)422);
                    await r422.WriteAsJsonAsync(new { errors = validationErrors });
                    _logger.LogWarning("Validation failed: {Errors}", string.Join(";", validationErrors));
                    return r422;
                }

                // Insert into DB
                var token = _tokenGenerator.GenerateToken(6);
                try
                {
                    var travelcardId = await _dbHelper.InsertTravelcardAsync(request, token);
                    var r201 = req.CreateResponse(System.Net.HttpStatusCode.Created);
                    await r201.WriteAsJsonAsync(new CreateTravelcardResponse { TravelcardId = travelcardId.ToString(), Token = token });
                    _logger.LogInformation("Exit CreateTravelcard success. TravelcardId={TravelcardId}", travelcardId);
                    return r201;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Database error while inserting travelcard");
                    var r500 = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                    await r500.WriteAsJsonAsync(new ErrorResponse { Error = "DatabaseError", Message = ex.Message });
                    return r500;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in CreateTravelcard");
                var r500 = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await r500.WriteAsJsonAsync(new ErrorResponse { Error = "UnhandledError", Message = ex.Message });
                return r500;
            }
        }
    }
}
