using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using travelcard_http.Helpers;
using travelcard_http.Models;

namespace travelcard_http.Functions
{
    public class CreateTravelcardFunction
    {
        private readonly ILogger _logger;
        private readonly DbHelper _dbHelper;
        private readonly IConfiguration _configuration;

        public CreateTravelcardFunction(ILoggerFactory loggerFactory, DbHelper dbHelper, IConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<CreateTravelcardFunction>();
            _dbHelper = dbHelper;
            _configuration = configuration;
        }

        /// <summary>
        /// HTTP POST function to create a new travelcard and its cardholder(s).
        /// Validates headers, body and writes to PostgreSQL via DbHelper.
        /// Returns travelcardId and a short token on success.
        /// </summary>
        [Function("CreateTravelcard")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "travelcard")] HttpRequestData req)
        {
            var start = DateTime.UtcNow;
            _logger.LogInformation("Enter CreateTravelcard at {Time}", start);

            try
            {
                // Header validation
                if (!req.Headers.TryGetValues("client_id", out var clientIds) || string.IsNullOrWhiteSpace(clientIds?.FirstOrDefault()))
                {
                    _logger.LogWarning("client_id header missing or empty");
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteAsJsonAsync(new ErrorResponse { Code = "MissingClientId", Message = "client_id header is required." });
                    return bad;
                }

                if (req.Body == null || req.Headers.GetValues("Content-Type").FirstOrDefault()?.Contains("application/json", StringComparison.OrdinalIgnoreCase) != true)
                {
                    _logger.LogWarning("Invalid Content-Type or empty body");
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteAsJsonAsync(new ErrorResponse { Code = "InvalidContentType", Message = "Content-Type must be application/json and body must be provided." });
                    return bad;
                }

                string body;
                using (var sr = new StreamReader(req.Body))
                {
                    body = await sr.ReadToEndAsync();
                }

                TravelcardRequest? model;
                try
                {
                    var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    opts.Converters.Add(new JsonStringEnumConverter());
                    model = JsonSerializer.Deserialize<TravelcardRequest>(body, opts);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Invalid JSON payload");
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteAsJsonAsync(new ErrorResponse { Code = "InvalidJson", Message = "Request body is not a valid JSON payload." });
                    return bad;
                }

                if (model == null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteAsJsonAsync(new ErrorResponse { Code = "InvalidPayload", Message = "Request body could not be parsed." });
                    return bad;
                }

                // Business validations
                var now = DateTime.UtcNow;

                if (model.TravelcardRequestedDate == default)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteAsJsonAsync(new ErrorResponse { Code = "MissingRequestedDate", Message = "travelcardRequestedDate is required." });
                    return bad;
                }

                if (model.TravelcardRequestedDate > now)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteAsJsonAsync(new ErrorResponse { Code = "RequestedDateInFuture", Message = "travelcardRequestedDate must be in the past." });
                    return bad;
                }

                if (model.TravelcardValidFrom == default || model.TravelcardValidTo == default)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteAsJsonAsync(new ErrorResponse { Code = "MissingValidFromOrTo", Message = "travelcardValidFrom and travelcardValidTo are required." });
                    return bad;
                }

                if (model.TravelcardValidFrom >= model.TravelcardValidTo)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteAsJsonAsync(new ErrorResponse { Code = "InvalidValidityRange", Message = "travelcardValidFrom must be earlier than travelcardValidTo." });
                    return bad;
                }

                if (model.TravelcardValidTo <= now)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteAsJsonAsync(new ErrorResponse { Code = "ValidToInPast", Message = "travelcardValidTo must be in the future." });
                    return bad;
                }

                if (model.TravelcardType == TravelcardType.SixteenToSeventeen)
                {
                    if (model.TravelcardUsableTo == null || model.TravelcardUsableTo == default)
                    {
                        var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                        await bad.WriteAsJsonAsync(new ErrorResponse { Code = "MissingUsableTo", Message = "travelcardUsableTo is required for SixteenToSeventeen travelcards." });
                        return bad;
                    }

                    if (model.TravelcardUsableTo <= now)
                    {
                        var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                        await bad.WriteAsJsonAsync(new ErrorResponse { Code = "UsableToInPast", Message = "travelcardUsableTo must be in the future." });
                        return bad;
                    }
                }

                if (string.IsNullOrWhiteSpace(model.TravelcardNumber) || model.TravelcardNumber.Length < 11 || model.TravelcardNumber.Length > 22)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteAsJsonAsync(new ErrorResponse { Code = "InvalidNumber", Message = "travelcardNumber must be between 11 and 22 characters." });
                    return bad;
                }

                if (string.IsNullOrWhiteSpace(model.TravelcardTransactionReference) || model.TravelcardTransactionReference.Length != 15)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteAsJsonAsync(new ErrorResponse { Code = "InvalidTransactionReference", Message = "travelcardTransactionReference must be exactly 15 characters." });
                    return bad;
                }

                if (model.Cardholders == null || model.Cardholders.Count < 1 || model.Cardholders.Count > 2)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteAsJsonAsync(new ErrorResponse { Code = "InvalidCardholders", Message = "cardholders must contain exactly 1 or 2 cardholders." });
                    return bad;
                }

                // Enforce allowed secondary only for certain travelcard types
                var secondaryCount = model.Cardholders.Count(c => c.CardholderType == CardholderType.Secondary);
                if (secondaryCount > 0)
                {
                    var allowedSecondaryTypes = new[] { TravelcardType.TwoTogether, TravelcardType.Family };
                    if (!allowedSecondaryTypes.Contains(model.TravelcardType))
                    {
                        var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                        await bad.WriteAsJsonAsync(new ErrorResponse { Code = "SecondaryNotAllowed", Message = "Secondary cardholder is not allowed for this travelcard type." });
                        return bad;
                    }
                }

                // Cardholder image one-of validation and length checks
                foreach (var ch in model.Cardholders)
                {
                    if (string.IsNullOrWhiteSpace(ch.CardholderTitle) || ch.CardholderTitle.Length > 15)
                    {
                        var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                        await bad.WriteAsJsonAsync(new ErrorResponse { Code = "InvalidCardholderTitle", Message = "cardholderTitle is required and must be <= 15 characters." });
                        return bad;
                    }

                    if (string.IsNullOrWhiteSpace(ch.CardholderForename) || ch.CardholderForename.Length > 100)
                    {
                        var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                        await bad.WriteAsJsonAsync(new ErrorResponse { Code = "InvalidCardholderForename", Message = "cardholderForename is required and must be <= 100 characters." });
                        return bad;
                    }

                    if (string.IsNullOrWhiteSpace(ch.CardholderSurname) || ch.CardholderSurname.Length > 100)
                    {
                        var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                        await bad.WriteAsJsonAsync(new ErrorResponse { Code = "InvalidCardholderSurname", Message = "cardholderSurname is required and must be <= 100 characters." });
                        return bad;
                    }

                    if (string.IsNullOrWhiteSpace(ch.CardholderPhotoName) || ch.CardholderPhotoName.Length > 100)
                    {
                        var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                        await bad.WriteAsJsonAsync(new ErrorResponse { Code = "InvalidPhotoName", Message = "cardholderPhotoName is required and must be <= 100 characters." });
                        return bad;
                    }

                    var providedImageFields = new[] { !string.IsNullOrWhiteSpace(ch.CardholderPhotoRrsKey), !string.IsNullOrWhiteSpace(ch.CardholderPhotoUrl), !string.IsNullOrWhiteSpace(ch.CardholderPhotoKey) };
                    if (providedImageFields.Count(x => x) != 1)
                    {
                        var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                        await bad.WriteAsJsonAsync(new ErrorResponse { Code = "InvalidCardholderPhoto", Message = "Each cardholder must provide exactly one of cardholderPhotoRRSKey, cardholderPhotoURL or cardholderPhotoKey." });
                        return bad;
                    }

                    if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoRrsKey) && (ch.CardholderPhotoRrsKey.Length < 39 || ch.CardholderPhotoRrsKey.Length > 42))
                    {
                        var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                        await bad.WriteAsJsonAsync(new ErrorResponse { Code = "InvalidPhotoRRSKey", Message = "cardholderPhotoRRSKey must be between 39 and 42 characters if provided." });
                        return bad;
                    }

                    if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoKey) && (ch.CardholderPhotoKey.Length < 39 || ch.CardholderPhotoKey.Length > 42))
                    {
                        var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                        await bad.WriteAsJsonAsync(new ErrorResponse { Code = "InvalidPhotoKey", Message = "cardholderPhotoKey must be between 39 and 42 characters if provided." });
                        return bad;
                    }

                    if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoUrl) && (ch.CardholderPhotoUrl.Length < 20 || ch.CardholderPhotoUrl.Length > 2048))
                    {
                        var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                        await bad.WriteAsJsonAsync(new ErrorResponse { Code = "InvalidPhotoUrl", Message = "cardholderPhotoURL must be a valid URL between 20 and 2048 characters if provided." });
                        return bad;
                    }
                }

                // Persist to DB
                var created = await _dbHelper.InsertTravelcardAsync(model);

                var resp = req.CreateResponse(HttpStatusCode.Created);
                var result = new TravelcardResponse { TravelcardId = created.ToString(), Token = GenerateToken(6) };
                await resp.WriteAsJsonAsync(result);

                var end = DateTime.UtcNow;
                _logger.LogInformation("Exit CreateTravelcard at {Time} DurationMs={Duration}", end, (end - start).TotalMilliseconds);
                return resp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in CreateTravelcard");
                var resp = req.CreateResponse(HttpStatusCode.InternalServerError);
                await resp.WriteAsJsonAsync(new ErrorResponse { Code = "InternalError", Message = "An unexpected error occurred." });
                return resp;
            }
        }

        private static string GenerateToken(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var rng = new Random();
            return new string(Enumerable.Range(0, length).Select(_ => chars[rng.Next(chars.Length)]).ToArray());
        }
    }
}
