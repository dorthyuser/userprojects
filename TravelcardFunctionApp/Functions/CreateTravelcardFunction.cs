using System;
using System.IO;
using System.Net;
using System.Text.Json;
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
        private readonly DatabaseHelper _dbHelper;
        private readonly ILogger<CreateTravelcardFunction> _logger;

        public CreateTravelcardFunction(DatabaseHelper dbHelper, ILogger<CreateTravelcardFunction> logger)
        {
            _dbHelper = dbHelper;
            _logger = logger;
        }

        [Function("CreateTravelcard")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "travelcard")] HttpRequestData req, FunctionContext executionContext)
        {
            var invocationId = executionContext.InvocationId?.ToString() ?? Guid.NewGuid().ToString();
            _logger.LogInformation("Entry CreateTravelcard - InvocationId: {InvocationId}", invocationId);

            try
            {
                // Header validation
                if (!req.Headers.TryGetValues("client_id", out var clientIdValues))
                {
                    _logger.LogWarning("Missing client_id header - InvocationId: {InvocationId}", invocationId);
                    var resp400 = req.CreateResponse(HttpStatusCode.BadRequest);
                    await resp400.WriteAsJsonAsync(new { errors = new[] { "Missing required header: client_id" }, correlationId = invocationId });
                    return resp400;
                }

                var clientId = string.Empty;
                foreach (var v in clientIdValues)
                {
                    clientId = v;
                    break;
                }

                if (string.IsNullOrWhiteSpace(clientId) || clientId.Length > 128)
                {
                    _logger.LogWarning("Invalid client_id header - InvocationId: {InvocationId}", invocationId);
                    var resp400 = req.CreateResponse(HttpStatusCode.BadRequest);
                    await resp400.WriteAsJsonAsync(new { errors = new[] { "Invalid client_id header" }, correlationId = invocationId });
                    return resp400;
                }

                if (!req.Headers.TryGetValues("Content-Type", out var contentTypeValues))
                {
                    _logger.LogWarning("Missing Content-Type header - InvocationId: {InvocationId}", invocationId);
                    var resp415 = req.CreateResponse(HttpStatusCode.UnsupportedMediaType);
                    await resp415.WriteAsJsonAsync(new { errors = new[] { "Content-Type header required and must be application/json" }, correlationId = invocationId });
                    return resp415;
                }

                var contentType = string.Empty;
                foreach (var v in contentTypeValues)
                {
                    contentType = v;
                    break;
                }

                if (!contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Invalid Content-Type header - InvocationId: {InvocationId}", invocationId);
                    var resp415 = req.CreateResponse(HttpStatusCode.UnsupportedMediaType);
                    await resp415.WriteAsJsonAsync(new { errors = new[] { "Content-Type must be application/json" }, correlationId = invocationId });
                    return resp415;
                }

                string body = string.Empty;
                using (var sr = new StreamReader(req.Body))
                {
                    body = await sr.ReadToEndAsync();
                }

                if (string.IsNullOrWhiteSpace(body))
                {
                    _logger.LogWarning("Empty request body - InvocationId: {InvocationId}", invocationId);
                    var resp400 = req.CreateResponse(HttpStatusCode.BadRequest);
                    await resp400.WriteAsJsonAsync(new { errors = new[] { "Request body is required" }, correlationId = invocationId });
                    return resp400;
                }

                TravelcardRequest request;
                try
                {
                    request = JsonSerializer.Deserialize<TravelcardRequest>(body, DatabaseHelper.SerializerOptions) ?? new TravelcardRequest();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Malformed JSON - InvocationId: {InvocationId}", invocationId);
                    var resp400 = req.CreateResponse(HttpStatusCode.BadRequest);
                    await resp400.WriteAsJsonAsync(new { errors = new[] { "Malformed JSON payload" }, correlationId = invocationId });
                    return resp400;
                }

                // Validation
                var validationErrors = Validators.ValidateTravelcardRequest(request);
                if (validationErrors.Count > 0)
                {
                    _logger.LogWarning("Validation failed - InvocationId: {InvocationId} Errors: {Errors}", invocationId, string.Join(';', validationErrors));
                    var resp422 = req.CreateResponse((HttpStatusCode)422);
                    await resp422.WriteAsJsonAsync(new { errors = validationErrors, correlationId = invocationId });
                    return resp422;
                }

                // Persist to DB
                var (dbId, token) = await _dbHelper.CreateTravelcardAsync(request);

                var responsePayload = new { travelcardId = Guid.NewGuid().ToString(), token };

                var resp201 = req.CreateResponse(HttpStatusCode.Created);
                resp201.Headers.Add("Content-Type", "application/json");
                var json = JsonSerializer.Serialize(responsePayload, DatabaseHelper.SerializerOptions);
                await using (var writer = new StreamWriter(resp201.Body))
                {
                    await writer.WriteAsync(json);
                    await writer.FlushAsync();
                }

                _logger.LogInformation("Exit CreateTravelcard - InvocationId: {InvocationId} TravelcardDbId: {DbId}", invocationId, dbId);
                return resp201;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in CreateTravelcard - InvocationId: {InvocationId}", invocationId);
                var resp500 = req.CreateResponse(HttpStatusCode.InternalServerError);
                await resp500.WriteAsJsonAsync(new { errors = new[] { "Internal server error" }, correlationId = invocationId });
                return resp500;
            }
        }
    }
}
