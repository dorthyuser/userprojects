using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using TravelCardFunctionApp.Repositories;
using TravelCardFunctionApp.Helpers;

namespace TravelCardFunctionApp.Functions
{
    public class GetTravelCardFunction
    {
        private readonly TravelCardRepository _repository;
        private readonly ILogger<GetTravelCardFunction> _logger;
        private readonly string _apiKey;

        public GetTravelCardFunction(TravelCardRepository repository, ILogger<GetTravelCardFunction> logger, Microsoft.Extensions.Configuration.IConfiguration config)
        {
            _repository = repository;
            _logger = logger;
            _apiKey = config["FunctionApiKey"] ?? string.Empty;
        }

        [Function("GetTravelCard")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "GET", Route = "travelcard")] HttpRequestData req, FunctionContext executionContext)
        {
            var traceId = executionContext.InvocationId.ToString();

            try
            {
                // Basic API key auth for backend integration security
                if (string.IsNullOrWhiteSpace(_apiKey))
                {
                    var errNoKey = new ErrorResponse
                    {
                        ErrorCode = "ServerConfiguration",
                        Message = "Function API key not configured on the server.",
                        Details = "Set FunctionApiKey in configuration.",
                        TraceId = traceId
                    };
                    var respNoKey = req.CreateResponse(HttpStatusCode.InternalServerError);
                    respNoKey.Headers.Add("Content-Type", "application/json");
                    await respNoKey.WriteStringAsync(JsonSerializer.Serialize(errNoKey));
                    return respNoKey;
                }

                if (!req.Headers.TryGetValues("x-api-key", out var values) || string.IsNullOrWhiteSpace(System.Linq.Enumerable.FirstOrDefault(values)))
                {
                    var unauthorized = new ErrorResponse
                    {
                        ErrorCode = "Unauthorized",
                        Message = "Missing API key header 'x-api-key'.",
                        Details = "Provide a valid API key in the x-api-key header.",
                        TraceId = traceId
                    };
                    var respUnauthorized = req.CreateResponse(HttpStatusCode.Unauthorized);
                    respUnauthorized.Headers.Add("Content-Type", "application/json");
                    await respUnauthorized.WriteStringAsync(JsonSerializer.Serialize(unauthorized));
                    return respUnauthorized;
                }

                var providedKey = System.Linq.Enumerable.FirstOrDefault(values) ?? string.Empty;
                if (providedKey != _apiKey)
                {
                    var unauthorized = new ErrorResponse
                    {
                        ErrorCode = "Unauthorized",
                        Message = "Invalid API key.",
                        Details = "The provided API key is invalid.",
                        TraceId = traceId
                    };
                    var respUnauthorized = req.CreateResponse(HttpStatusCode.Unauthorized);
                    respUnauthorized.Headers.Add("Content-Type", "application/json");
                    await respUnauthorized.WriteStringAsync(JsonSerializer.Serialize(unauthorized));
                    return respUnauthorized;
                }

                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var cardIdStr = query.Get("cardId");
                if (string.IsNullOrWhiteSpace(cardIdStr) || !Guid.TryParse(cardIdStr, out var cardId))
                {
                    var badReq = new ErrorResponse
                    {
                        ErrorCode = "BadRequest",
                        Message = "cardId is missing or invalid GUID",
                        Details = "Provide a valid cardId query parameter as a GUID.",
                        TraceId = traceId
                    };
                    var respBad = req.CreateResponse(HttpStatusCode.BadRequest);
                    respBad.Headers.Add("Content-Type", "application/json");
                    await respBad.WriteStringAsync(JsonSerializer.Serialize(badReq));
                    return respBad;
                }

                var card = await _repository.GetByIdAsync(cardId);
                if (card == null)
                {
                    var notFound = new ErrorResponse
                    {
                        ErrorCode = "NotFound",
                        Message = "Travel card not found",
                        Details = $"No travel card found with id {cardId}",
                        TraceId = traceId
                    };
                    var respNotFound = req.CreateResponse(HttpStatusCode.NotFound);
                    respNotFound.Headers.Add("Content-Type", "application/json");
                    await respNotFound.WriteStringAsync(JsonSerializer.Serialize(notFound));
                    return respNotFound;
                }

                var ok = req.CreateResponse(HttpStatusCode.OK);
                ok.Headers.Add("Content-Type", "application/json");
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                };
                await ok.WriteStringAsync(System.Text.Json.JsonSerializer.Serialize(card, options));
                return ok;
            }
            catch (Exception ex)
            {
                LoggingHelper.LogException(_logger, ex, traceId);
                var err = new ErrorResponse
                {
                    ErrorCode = "InternalServerError",
                    Message = "An unexpected error occurred.",
                    Details = ex.Message,
                    TraceId = traceId
                };
                var resp = req.CreateResponse(HttpStatusCode.InternalServerError);
                resp.Headers.Add("Content-Type", "application/json");
                await resp.WriteStringAsync(JsonSerializer.Serialize(err));
                return resp;
            }
        }
    }
}
