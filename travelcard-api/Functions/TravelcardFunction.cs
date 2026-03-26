using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using TravelcardApi.Models;
using TravelcardApi.Services;

namespace TravelcardApi.Functions
{
    public class TravelcardFunction
    {
        private readonly ILogger _logger;
        private readonly TravelcardService _service;

        public TravelcardFunction(ILogger<TravelcardFunction> logger, TravelcardService service)
        {
            _logger = logger;
            _service = service;
        }

        [Function("CreateTravelcard")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "travelcard")] HttpRequestData req,
            FunctionContext context)
        {
            _logger.LogInformation("Enter CreateTravelcard");
            try
            {
                if (!req.Headers.TryGetValues("client_id", out var clientIdValues))
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    var errObj = new { error = "Missing required header: client_id" };
                    await bad.WriteAsJsonAsync(errObj);
                    _logger.LogError("Missing client_id header");
                    return bad;
                }

                var clientId = System.Linq.Enumerable.FirstOrDefault(clientIdValues) ?? string.Empty;
                if (clientId.Length < 1 || clientId.Length > 128)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    var errObj = new { error = "Invalid client_id header" };
                    await bad.WriteAsJsonAsync(errObj);
                    _logger.LogError("Invalid client_id header length");
                    return bad;
                }

                // Read body
                var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    PropertyNameCaseInsensitive = true
                };

                TravelcardRequest payload;
                try
                {
                    payload = await JsonSerializer.DeserializeAsync<TravelcardRequest>(req.Body, options) ?? new TravelcardRequest();
                }
                catch (Exception ex)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    var errObj = new { error = "Invalid JSON payload", detail = ex.Message };
                    await bad.WriteAsJsonAsync(errObj);
                    _logger.LogError(ex, "Failed to deserialize payload");
                    return bad;
                }

                // call service
                var result = await _service.CreateTravelcardAsync(payload, clientId, req.Headers.TryGetValues("X-Correlation-Cust-Id", out var corr) ? System.Linq.Enumerable.FirstOrDefault(corr) : null);

                var response = req.CreateResponse(HttpStatusCode.Created);
                await response.WriteAsJsonAsync(result);
                _logger.LogInformation("Exit CreateTravelcard Success travelcardId={TravelcardId}", result.travelcardId);
                return response;
            }
            catch (ValidationException vex)
            {
                var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                await resp.WriteAsJsonAsync(new { error = "ValidationFailed", details = vex.Errors });
                _logger.LogError("Validation failed: {Errors}", string.Join(';', vex.Errors));
                return resp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in CreateTravelcard");
                var resp = req.CreateResponse(HttpStatusCode.InternalServerError);
                await resp.WriteAsJsonAsync(new { error = "InternalServerError", detail = ex.Message });
                return resp;
            }
        }
    }
}
