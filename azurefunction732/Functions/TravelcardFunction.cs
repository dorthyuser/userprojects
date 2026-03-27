using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using TravelcardService.Helpers;
using TravelcardService.Models;

namespace TravelcardService.Functions
{
    public class TravelcardFunction
    {
        private readonly ILogger<TravelcardFunction> _logger;
        private readonly DbHelper _dbHelper;
        private readonly Validator _validator;

        public TravelcardFunction(ILogger<TravelcardFunction> logger, DbHelper dbHelper, Validator validator)
        {
            _logger = logger;
            _dbHelper = dbHelper;
            _validator = validator;
        }

        [Function("CreateTravelcard")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "travelcard")] HttpRequestData req)
        {
            _logger.LogInformation("Enter CreateTravelcard: {Method} {Url}", req.Method, req.Url);
            try
            {
                // Header checks
                if (!req.Headers.TryGetValues("client_id", out var clientIds) || clientIds == null || string.IsNullOrWhiteSpace(clientIds.FirstOrDefault()))
                {
                    var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    var error = new { error = "MissingRequiredHeader", message = "client_id header is required and must be 1-128 characters" };
                    await bad.WriteAsJsonAsync(error);
                    _logger.LogWarning("client_id header missing");
                    return bad;
                }

                var clientId = clientIds.FirstOrDefault() ?? string.Empty;
                if (clientId.Length > 128)
                {
                    var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    var error = new { error = "InvalidHeader", message = "client_id length must be <= 128" };
                    await bad.WriteAsJsonAsync(error);
                    _logger.LogWarning("client_id header too long");
                    return bad;
                }

                if (req.Headers.TryGetValues("X-Correlation-Cust-Id", out var corrVals))
                {
                    var corr = corrVals.FirstOrDefault();
                    if (corr != null && corr.Length > 100)
                    {
                        var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                        var error = new { error = "InvalidHeader", message = "X-Correlation-Cust-Id must be <= 100 characters" };
                        await bad.WriteAsJsonAsync(error);
                        _logger.LogWarning("X-Correlation-Cust-Id too long");
                        return bad;
                    }
                }

                if (req.Headers.TryGetValues("Content-Type", out var ctVals))
                {
                    var ct = ctVals.FirstOrDefault() ?? string.Empty;
                    if (!ct.Contains("application/json", StringComparison.OrdinalIgnoreCase))
                    {
                        var bad = req.CreateResponse(System.Net.HttpStatusCode.UnsupportedMediaType);
                        var error = new { error = "UnsupportedMediaType", message = "Content-Type must be application/json" };
                        await bad.WriteAsJsonAsync(error);
                        _logger.LogWarning("Unsupported content type: {ContentType}", ct);
                        return bad;
                    }
                }

                // parse body
                TravelcardRequest? requestModel;
                try
                {
                    using var sr = new StreamReader(req.Body);
                    var body = await sr.ReadToEndAsync();
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                    requestModel = JsonSerializer.Deserialize<TravelcardRequest>(body, options);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to deserialize request body");
                    var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    var error = new { error = "InvalidJson", message = "Request body is not valid JSON" };
                    await bad.WriteAsJsonAsync(error);
                    return bad;
                }

                if (requestModel == null)
                {
                    var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    var error = new { error = "InvalidPayload", message = "Request body is empty or invalid" };
                    await bad.WriteAsJsonAsync(error);
                    _logger.LogWarning("Request model null after deserialization");
                    return bad;
                }

                var validationErrors = _validator.Validate(requestModel);
                if (validationErrors.Any())
                {
                    var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    var error = new { error = "ValidationFailed", details = validationErrors };
                    await bad.WriteAsJsonAsync(error);
                    _logger.LogWarning("Validation failed: {Errors}", string.Join(";", validationErrors));
                    return bad;
                }

                // Insert into DB
                var travelcardGuid = Guid.NewGuid().ToString();
                var token = _validator.GenerateToken(6);

                var dbResult = await _dbHelper.InsertTravelcardAsync(requestModel, travelcardGuid);

                var response = req.CreateResponse(System.Net.HttpStatusCode.Created);
                await response.WriteAsJsonAsync(new { travelcardId = travelcardGuid, token = token });
                _logger.LogInformation("Exit CreateTravelcard: success travelcardId={TravelcardId} dbId={DbId}", travelcardGuid, dbResult.TravelcardDbId);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in CreateTravelcard");
                var resp = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                var error = new { error = "InternalServerError", message = "An unexpected error occurred" };
                await resp.WriteAsJsonAsync(error);
                return resp;
            }
        }
    }
}
