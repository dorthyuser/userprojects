using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using CreateTravelcard.Models;
using CreateTravelcard.Validators;
using CreateTravelcard.Helpers;
using System.Linq;

namespace CreateTravelcard.Functions
{
    public class CreateTravelcardFunction
    {
        private readonly ILogger _logger;
        private readonly TravelcardValidator _validator;
        private readonly DbHelper _dbHelper;

        public CreateTravelcardFunction(ILoggerFactory loggerFactory, TravelcardValidator validator, DbHelper dbHelper)
        {
            _logger = loggerFactory.CreateLogger<CreateTravelcardFunction>();
            _validator = validator;
            _dbHelper = dbHelper;
        }

        [Function("CreateTravelcard")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "api/travelcards")] HttpRequestData req)
        {
            var correlationId = string.Empty;
            try
            {
                // Headers
                if (!req.Headers.TryGetValues("client_id", out var clientIds))
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteAsJsonAsync(new ErrorResponse
                    {
                        CorrelationId = null,
                        Errors = new[] { new ErrorItem { Code = "MissingHeader", Field = "client_id", Message = "Header 'client_id' is required." } }
                    });
                    return bad;
                }

                var clientId = clientIds.FirstOrDefault();
                if (string.IsNullOrEmpty(clientId) || clientId.Length < 1 || clientId.Length > 128)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteAsJsonAsync(new ErrorResponse
                    {
                        CorrelationId = null,
                        Errors = new[] { new ErrorItem { Code = "InvalidHeader", Field = "client_id", Message = "Header 'client_id' must be between 1 and 128 characters." } }
                    });
                    return bad;
                }

                if (req.Headers.TryGetValues("X-Correlation-Cust-Id", out var corr))
                {
                    correlationId = corr.FirstOrDefault();
                    if (!string.IsNullOrEmpty(correlationId) && correlationId.Length > 100)
                    {
                        var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                        await bad.WriteAsJsonAsync(new ErrorResponse
                        {
                            CorrelationId = correlationId,
                            Errors = new[] { new ErrorItem { Code = "InvalidHeader", Field = "X-Correlation-Cust-Id", Message = "Header 'X-Correlation-Cust-Id' must be at most 100 characters." } }
                        });
                        return bad;
                    }
                }

                // Content-Type validation
                if (req.Body == null || req.Body == Stream.Null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteAsJsonAsync(new ErrorResponse
                    {
                        CorrelationId = correlationId,
                        Errors = new[] { new ErrorItem { Code = "EmptyBody", Field = "body", Message = "Request body is required and must be application/json." } }
                    });
                    return bad;
                }

                if (!req.Headers.TryGetValues("Content-Type", out var cts) || !cts.Any(h => h.Contains("application/json", StringComparison.OrdinalIgnoreCase)))
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteAsJsonAsync(new ErrorResponse
                    {
                        CorrelationId = correlationId,
                        Errors = new[] { new ErrorItem { Code = "InvalidContentType", Field = "Content-Type", Message = "Content-Type must be application/json." } }
                    });
                    return bad;
                }

                // Read body
                TravelcardRequest payload;
                try
                {
                    payload = await JsonSerializer.DeserializeAsync<TravelcardRequest>(req.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to deserialize request body");
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteAsJsonAsync(new ErrorResponse
                    {
                        CorrelationId = correlationId,
                        Errors = new[] { new ErrorItem { Code = "InvalidJson", Field = "body", Message = "Request body is not valid JSON or does not match the schema." } }
                    });
                    return bad;
                }

                // Validate
                var validationErrors = _validator.Validate(payload);
                if (validationErrors.Any())
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteAsJsonAsync(new ErrorResponse
                    {
                        CorrelationId = correlationId,
                        Errors = validationErrors.ToArray()
                    });
                    return bad;
                }

                // Additional business validations involving dates and cardholder counts
                var bizErrors = _validator.ValidateBusinessRules(payload);
                if (bizErrors.Any())
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteAsJsonAsync(new ErrorResponse
                    {
                        CorrelationId = correlationId,
                        Errors = bizErrors.ToArray()
                    });
                    return bad;
                }

                // Persist
                var (travelcardId, token) = await _dbHelper.InsertTravelcardAsync(payload, clientId, correlationId);

                var created = req.CreateResponse(HttpStatusCode.Created);
                created.Headers.Add("Content-Type", "application/json");
                var resp = new { travelcardId = travelcardId.ToString(), token };
                await created.WriteStringAsync(JsonSerializer.Serialize(resp));
                return created;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in CreateTravelcard function. CorrelationId: {CorrelationId}", correlationId);
                var resp = req.CreateResponse(HttpStatusCode.InternalServerError);
                await resp.WriteAsJsonAsync(new ErrorResponse
                {
                    CorrelationId = correlationId,
                    Errors = new[] { new ErrorItem { Code = "ServerError", Field = null, Message = "An unexpected error occurred." } }
                });
                return resp;
            }
        }
    }
}
