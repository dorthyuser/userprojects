using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
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
        private readonly ILogger<CreateTravelcardFunction> _logger;

        public CreateTravelcardFunction(DbHelper dbHelper, ILogger<CreateTravelcardFunction> logger)
        {
            _dbHelper = dbHelper;
            _logger = logger;
        }

        [Function("CreateTravelcard")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "travelcard")] HttpRequestData req,
            FunctionContext executionContext)
        {
            _logger.LogInformation("Entered CreateTravelcard function");
            try
            {
                // Header auth/validation
                if (!req.Headers.TryGetValues("client_id", out var clientIds))
                {
                    var resp401 = req.CreateResponse(HttpStatusCode.Unauthorized);
                    var err401 = new ErrorResponse { Error = "Missing client_id header" };
                    await resp401.WriteAsJsonAsync(err401);
                    _logger.LogWarning("Missing client_id header");
                    return resp401;
                }

                var clientId = clientIds.FirstOrDefault() ?? string.Empty;
                if (clientId.Length < 1 || clientId.Length > 128)
                {
                    var resp400 = req.CreateResponse(HttpStatusCode.BadRequest);
                    var err = new ErrorResponse { Error = "Invalid client_id header length" };
                    await resp400.WriteAsJsonAsync(err);
                    _logger.LogWarning("Invalid client_id header length");
                    return resp400;
                }

                if (req.Headers.TryGetValues("Content-Type", out var contentTypes))
                {
                    var ct = contentTypes.FirstOrDefault() ?? string.Empty;
                    if (!ct.Contains("application/json", StringComparison.OrdinalIgnoreCase))
                    {
                        var resp415 = req.CreateResponse(HttpStatusCode.UnsupportedMediaType);
                        var err = new ErrorResponse { Error = "Content-Type must be application/json" };
                        await resp415.WriteAsJsonAsync(err);
                        _logger.LogWarning("Unsupported Content-Type: {ContentType}", ct);
                        return resp415;
                    }
                }

                var body = await new StreamReader(req.Body).ReadToEndAsync();
                TravelcardRequest? payload = null;
                try
                {
                    payload = JsonSerializer.Deserialize<TravelcardRequest>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (Exception ex)
                {
                    var resp400 = req.CreateResponse(HttpStatusCode.BadRequest);
                    var err = new ErrorResponse { Error = "Invalid JSON payload", Details = ex.Message };
                    await resp400.WriteAsJsonAsync(err);
                    _logger.LogError(ex, "Invalid JSON payload");
                    return resp400;
                }

                if (payload == null)
                {
                    var resp400 = req.CreateResponse(HttpStatusCode.BadRequest);
                    var err = new ErrorResponse { Error = "Empty payload" };
                    await resp400.WriteAsJsonAsync(err);
                    _logger.LogWarning("Empty payload");
                    return resp400;
                }

                var validation = payload.Validate();
                if (!validation.IsValid)
                {
                    var resp400 = req.CreateResponse(HttpStatusCode.BadRequest);
                    var err = new ErrorResponse { Error = "Validation failed", Details = validation.ErrorMessage };
                    await resp400.WriteAsJsonAsync(err);
                    _logger.LogWarning("Validation failed: {Reason}", validation.ErrorMessage);
                    return resp400;
                }

                // Business validations
                var now = DateTime.UtcNow;
                if (payload.TravelcardRequestedDate >= now)
                {
                    var resp400 = req.CreateResponse(HttpStatusCode.BadRequest);
                    var err = new ErrorResponse { Error = "travelcardRequestedDate must be in the past" };
                    await resp400.WriteAsJsonAsync(err);
                    _logger.LogWarning("travelcardRequestedDate must be in the past");
                    return resp400;
                }

                if (payload.TravelcardValidFrom >= payload.TravelcardValidTo)
                {
                    var resp400 = req.CreateResponse(HttpStatusCode.BadRequest);
                    var err = new ErrorResponse { Error = "travelcardValidFrom must be earlier than travelcardValidTo" };
                    await resp400.WriteAsJsonAsync(err);
                    _logger.LogWarning("travelcardValidFrom must be earlier than travelcardValidTo");
                    return resp400;
                }

                if (payload.TravelcardValidTo <= now)
                {
                    var resp400 = req.CreateResponse(HttpStatusCode.BadRequest);
                    var err = new ErrorResponse { Error = "travelcardValidTo must be in the future" };
                    await resp400.WriteAsJsonAsync(err);
                    _logger.LogWarning("travelcardValidTo must be in the future");
                    return resp400;
                }

                if (payload.TravelcardType == Enums.TravelcardType.SixteenToSeventeen)
                {
                    if (!payload.TravelcardUsableTo.HasValue)
                    {
                        var resp400 = req.CreateResponse(HttpStatusCode.BadRequest);
                        var err = new ErrorResponse { Error = "travelcardUsableTo is required for SixteenToSeventeen type" };
                        await resp400.WriteAsJsonAsync(err);
                        _logger.LogWarning("travelcardUsableTo missing for SixteenToSeventeen");
                        return resp400;
                    }

                    if (payload.TravelcardUsableTo <= now)
                    {
                        var resp400 = req.CreateResponse(HttpStatusCode.BadRequest);
                        var err = new ErrorResponse { Error = "travelcardUsableTo must be in the future" };
                        await resp400.WriteAsJsonAsync(err);
                        _logger.LogWarning("travelcardUsableTo must be in the future");
                        return resp400;
                    }
                }

                // Secondary cardholder allowed only for Family and TwoTogether
                var hasSecondary = payload.Cardholders.Any(c => c.CardholderType == Enums.CardholderType.Secondary);
                if (hasSecondary && payload.TravelcardType != Enums.TravelcardType.Family && payload.TravelcardType != Enums.TravelcardType.TwoTogether)
                {
                    var resp400 = req.CreateResponse(HttpStatusCode.BadRequest);
                    var err = new ErrorResponse { Error = "Secondary cardholder is not allowed for this travelcard type" };
                    await resp400.WriteAsJsonAsync(err);
                    _logger.LogWarning("Secondary cardholder not allowed for type {Type}", payload.TravelcardType);
                    return resp400;
                }

                // Persist to DB
                var (dbId, token) = await _dbHelper.InsertTravelcardAsync(payload, CancellationToken.None);

                var responseObj = new CreateResponse { TravelcardId = Guid.NewGuid().ToString(), Token = token };
                var resp = req.CreateResponse(HttpStatusCode.Created);
                await resp.WriteAsJsonAsync(responseObj);
                _logger.LogInformation("Exiting CreateTravelcard function successfully. DB id {DbId}", dbId);
                return resp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in CreateTravelcard");
                var resp500 = req.CreateResponse(HttpStatusCode.InternalServerError);
                var err = new ErrorResponse { Error = "Internal server error", Details = ex.Message };
                await resp500.WriteAsJsonAsync(err);
                return resp500;
            }
        }
    }
}
