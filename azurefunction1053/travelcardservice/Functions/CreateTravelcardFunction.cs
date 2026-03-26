using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using travelcardservice.Helpers;
using travelcardservice.Models;

namespace travelcardservice.Functions
{
    public class CreateTravelcardFunction
    {
        private readonly ILogger<CreateTravelcardFunction> _logger;
        private readonly DbHelper _dbHelper;

        public CreateTravelcardFunction(ILogger<CreateTravelcardFunction> logger, DbHelper dbHelper)
        {
            _logger = logger;
            _dbHelper = dbHelper;
        }

        [Function("CreateTravelcard")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "travelcards")] HttpRequestData req,
            FunctionContext context)
        {
            _logger.LogInformation("Enter CreateTravelcard - {CorrelationId}", req.Headers.Contains("X-Correlation-Cust-Id") ? string.Join(',', req.Headers.GetValues("X-Correlation-Cust-Id")) : "-");

            try
            {
                if (!req.Headers.Contains("client_id") || string.IsNullOrWhiteSpace(string.Join("", req.Headers.GetValues("client_id"))))
                {
                    var badResp = req.CreateResponse(HttpStatusCode.BadRequest);
                    var err = new ErrorResponse { ErrorCode = "MissingClientId", Message = "Required header 'client_id' is missing or empty." };
                    badResp.Headers.Add("Content-Type", "application/json");
                    await badResp.WriteStringAsync(JsonSerializer.Serialize(err));
                    _logger.LogWarning("Missing client_id header");
                    return badResp;
                }

                if (!req.Headers.Contains("Content-Type") || !string.Join("", req.Headers.GetValues("Content-Type")).Contains("application/json"))
                {
                    var badResp = req.CreateResponse(HttpStatusCode.UnsupportedMediaType);
                    var err = new ErrorResponse { ErrorCode = "InvalidContentType", Message = "Content-Type must be application/json." };
                    badResp.Headers.Add("Content-Type", "application/json");
                    await badResp.WriteStringAsync(JsonSerializer.Serialize(err));
                    _logger.LogWarning("Invalid Content-Type");
                    return badResp;
                }

                string requestBody;
                using (var sr = new StreamReader(req.Body))
                {
                    requestBody = await sr.ReadToEndAsync();
                }

                TravelcardRequest? payload;
                try
                {
                    payload = JsonSerializer.Deserialize<TravelcardRequest>(requestBody, DbHelper.JsonSerializerOptions);
                }
                catch (Exception ex)
                {
                    var badResp = req.CreateResponse(HttpStatusCode.BadRequest);
                    var err = new ErrorResponse { ErrorCode = "InvalidJson", Message = "Unable to parse JSON payload.", Details = ex.Message };
                    badResp.Headers.Add("Content-Type", "application/json");
                    await badResp.WriteStringAsync(JsonSerializer.Serialize(err));
                    _logger.LogError(ex, "JSON deserialization failed");
                    return badResp;
                }

                if (payload == null)
                {
                    var badResp = req.CreateResponse(HttpStatusCode.BadRequest);
                    var err = new ErrorResponse { ErrorCode = "InvalidPayload", Message = "Payload is empty or invalid." };
                    badResp.Headers.Add("Content-Type", "application/json");
                    await badResp.WriteStringAsync(JsonSerializer.Serialize(err));
                    _logger.LogWarning("Payload null after deserialization");
                    return badResp;
                }

                var validation = ValidationHelper.ValidateTravelcardRequest(payload);
                if (!validation.IsValid)
                {
                    var badResp = req.CreateResponse(HttpStatusCode.BadRequest);
                    var err = new ErrorResponse { ErrorCode = "ValidationFailed", Message = "Validation failed.", Details = validation.ErrorMessage };
                    badResp.Headers.Add("Content-Type", "application/json");
                    await badResp.WriteStringAsync(JsonSerializer.Serialize(err));
                    _logger.LogWarning("Validation failed: {Error}", validation.ErrorMessage);
                    return badResp;
                }

                var result = await _dbHelper.CreateTravelcardAsync(payload);

                var ok = req.CreateResponse(HttpStatusCode.Created);
                ok.Headers.Add("Content-Type", "application/json");
                var responseObj = new { travelcardId = result.TravelcardId, token = result.Token };
                await ok.WriteStringAsync(JsonSerializer.Serialize(responseObj));

                _logger.LogInformation("Exit CreateTravelcard - created id {Id}", result.TravelcardId);

                return ok;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in CreateTravelcard");
                var resp = req.CreateResponse(HttpStatusCode.InternalServerError);
                var err = new ErrorResponse { ErrorCode = "InternalError", Message = "An unexpected error occurred.", Details = ex.Message };
                resp.Headers.Add("Content-Type", "application/json");
                await resp.WriteStringAsync(JsonSerializer.Serialize(err));
                return resp;
            }
        }
    }
}
