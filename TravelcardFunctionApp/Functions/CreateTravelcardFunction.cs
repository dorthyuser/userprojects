using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using TravelcardFunctionApp.Models;
using TravelcardFunctionApp.Helpers;

namespace TravelcardFunctionApp.Functions
{
    public class CreateTravelcardFunction
    {
        private readonly DbHelper _dbHelper;
        private readonly ILogger<CreateTravelcardFunction> _logger;

        public CreateTravelcardFunction(DbHelper dbHelper, ILogger<CreateTravelcardFunction> logger)
        {
            _dbHelper = dbHelper ?? throw new ArgumentNullException(nameof(dbHelper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [Function("CreateTravelcard")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "travelcards")] HttpRequestData req, FunctionContext context)
        {
            var logger = _logger;
            logger.LogInformation("Enter CreateTravelcard - {TraceId}", context.InvocationId);

            try
            {
                // Header validation
                if (!req.Headers.TryGetValues("client_id", out var clientIds) || string.IsNullOrWhiteSpace(clientIds?.FirstOrDefault()))
                {
                    var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    var error = new ErrorResponse { Code = "MissingClientId", Message = "Header 'client_id' is required and must be between 1 and 128 characters." };
                    await WriteJsonAsync(bad, error);
                    logger.LogWarning("Missing client_id header");
                    return bad;
                }

                var clientId = clientIds.First();
                if (clientId.Length > 128)
                {
                    var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    var error = new ErrorResponse { Code = "InvalidClientId", Message = "Header 'client_id' must be <= 128 characters." };
                    await WriteJsonAsync(bad, error);
                    logger.LogWarning("client_id too long");
                    return bad;
                }

                // Content-Type validation
                if (req.Body == null || req.Body.CanRead == false)
                {
                    var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    var error = new ErrorResponse { Code = "EmptyBody", Message = "Request body is required." };
                    await WriteJsonAsync(bad, error);
                    logger.LogWarning("Empty body");
                    return bad;
                }

                if (!req.Headers.TryGetValues("Content-Type", out var contentTypes) || !contentTypes.Any(ct => ct.Contains("application/json", StringComparison.OrdinalIgnoreCase)))
                {
                    var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    var error = new ErrorResponse { Code = "InvalidContentType", Message = "Content-Type 'application/json' is required." };
                    await WriteJsonAsync(bad, error);
                    logger.LogWarning("Invalid content-type");
                    return bad;
                }

                req.Headers.TryGetValues("X-Correlation-Cust-Id", out var correlationIds);
                var correlationId = correlationIds?.FirstOrDefault();
                if (!string.IsNullOrEmpty(correlationId) && correlationId.Length > 100)
                {
                    var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    var error = new ErrorResponse { Code = "InvalidCorrelationId", Message = "X-Correlation-Cust-Id must be <= 100 characters." };
                    await WriteJsonAsync(bad, error);
                    logger.LogWarning("CorrelationId too long");
                    return bad;
                }

                // Read body
                string body;
                using (var reader = new StreamReader(req.Body, Encoding.UTF8))
                {
                    body = await reader.ReadToEndAsync();
                }

                if (string.IsNullOrWhiteSpace(body))
                {
                    var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    var error = new ErrorResponse { Code = "EmptyBody", Message = "Request body is empty." };
                    await WriteJsonAsync(bad, error);
                    logger.LogWarning("Empty body after read");
                    return bad;
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

                TravelcardRequest? request;
                try
                {
                    request = JsonSerializer.Deserialize<TravelcardRequest>(body, options);
                }
                catch (JsonException ex)
                {
                    var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    var error = new ErrorResponse { Code = "InvalidJson", Message = "Malformed JSON request." };
                    await WriteJsonAsync(bad, error);
                    logger.LogError(ex, "JSON deserialization error");
                    return bad;
                }

                if (request == null)
                {
                    var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    var error = new ErrorResponse { Code = "InvalidRequest", Message = "Request could not be parsed." };
                    await WriteJsonAsync(bad, error);
                    logger.LogWarning("Request null after deserialization");
                    return bad;
                }

                // Validate required fields
                var validation = ValidateRequest(request);
                if (!validation.IsValid)
                {
                    var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    await WriteJsonAsync(bad, new ErrorResponse { Code = "ValidationFailed", Message = validation.ErrorMessage });
                    logger.LogWarning("Validation failed: {Error}", validation.ErrorMessage);
                    return bad;
                }

                // Business validations
                var now = DateTime.UtcNow;

                if (request.TravelcardRequestedDate >= now)
                {
                    var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    await WriteJsonAsync(bad, new ErrorResponse { Code = "RequestedDateInvalid", Message = "travelcardRequestedDate must be in the past." });
                    logger.LogWarning("Requested date not in past");
                    return bad;
                }

                if (request.TravelcardValidFrom > request.TravelcardValidTo)
                {
                    var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    await WriteJsonAsync(bad, new ErrorResponse { Code = "DateRangeInvalid", Message = "travelcardValidFrom must be earlier than or equal to travelcardValidTo." });
                    logger.LogWarning("ValidFrom after ValidTo");
                    return bad;
                }

                if (request.TravelcardValidTo <= now)
                {
                    var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    await WriteJsonAsync(bad, new ErrorResponse { Code = "ValidToInvalid", Message = "travelcardValidTo must be in the future." });
                    logger.LogWarning("ValidTo not in future");
                    return bad;
                }

                if (request.TravelcardType == TravelcardType.SixteenToSeventeen)
                {
                    if (!request.TravelcardUsableTo.HasValue)
                    {
                        var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                        await WriteJsonAsync(bad, new ErrorResponse { Code = "UsableToRequired", Message = "travelcardUsableTo is required for TravelcardType 'SixteenToSeventeen'." });
                        logger.LogWarning("UsableTo missing for SixteenToSeventeen");
                        return bad;
                    }

                    if (request.TravelcardUsableTo <= now)
                    {
                        var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                        await WriteJsonAsync(bad, new ErrorResponse { Code = "UsableToInvalid", Message = "travelcardUsableTo must be in the future." });
                        logger.LogWarning("UsableTo not in future");
                        return bad;
                    }
                }

                // Cardholder primary/secondary rules
                if (request.Cardholders == null || request.Cardholders.Count < 1 || request.Cardholders.Count > 2)
                {
                    var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    await WriteJsonAsync(bad, new ErrorResponse { Code = "CardholdersCount", Message = "cardholders must contain exactly 1 or 2 items." });
                    logger.LogWarning("Invalid cardholder count");
                    return bad;
                }

                var primaryCount = request.Cardholders.Count(c => c.CardholderType == CardholderType.Primary);
                if (primaryCount != 1)
                {
                    var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    await WriteJsonAsync(bad, new ErrorResponse { Code = "PrimaryCardholderRequired", Message = "Exactly one Primary cardholder is required." });
                    logger.LogWarning("Primary cardholder count invalid: {Count}", primaryCount);
                    return bad;
                }

                var secondaryCount = request.Cardholders.Count(c => c.CardholderType == CardholderType.Secondary);
                if (secondaryCount > 1)
                {
                    var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    await WriteJsonAsync(bad, new ErrorResponse { Code = "TooManySecondaries", Message = "At most one Secondary cardholder is allowed." });
                    logger.LogWarning("Too many secondary cardholders");
                    return bad;
                }

                foreach (var ch in request.Cardholders)
                {
                    // One of photo fields must be provided
                    var provided = !string.IsNullOrWhiteSpace(ch.CardholderPhotoRrsKey) || !string.IsNullOrWhiteSpace(ch.CardholderPhotoUrl) || !string.IsNullOrWhiteSpace(ch.CardholderPhotoKey);
                    if (!provided)
                    {
                        var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                        await WriteJsonAsync(bad, new ErrorResponse { Code = "CardholderPhotoMissing", Message = "Each cardholder must provide one of cardholderPhotoRRSKey, cardholderPhotoURL, or cardholderPhotoKey." });
                        logger.LogWarning("Cardholder missing photo reference");
                        return bad;
                    }

                    if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoRrsKey) && (ch.CardholderPhotoRrsKey.Length < 39 || ch.CardholderPhotoRrsKey.Length > 42))
                    {
                        var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                        await WriteJsonAsync(bad, new ErrorResponse { Code = "PhotoRrsKeyInvalid", Message = "cardholderPhotoRRSKey must be between 39 and 42 characters when provided." });
                        logger.LogWarning("cardholderPhotoRRSKey length invalid");
                        return bad;
                    }

                    if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoKey) && (ch.CardholderPhotoKey.Length < 39 || ch.CardholderPhotoKey.Length > 42))
                    {
                        var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                        await WriteJsonAsync(bad, new ErrorResponse { Code = "PhotoKeyInvalid", Message = "cardholderPhotoKey must be between 39 and 42 characters when provided." });
                        logger.LogWarning("cardholderPhotoKey length invalid");
                        return bad;
                    }

                    if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoUrl) && (ch.CardholderPhotoUrl.Length < 20 || ch.CardholderPhotoUrl.Length > 2048))
                    {
                        var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                        await WriteJsonAsync(bad, new ErrorResponse { Code = "PhotoUrlInvalid", Message = "cardholderPhotoURL must be between 20 and 2048 characters when provided." });
                        logger.LogWarning("cardholderPhotoURL length invalid");
                        return bad;
                    }

                    if (string.IsNullOrWhiteSpace(ch.CardholderTitle) || ch.CardholderTitle.Length > 15)
                    {
                        var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                        await WriteJsonAsync(bad, new ErrorResponse { Code = "CardholderTitleInvalid", Message = "cardholderTitle is required and must be <= 15 characters." });
                        logger.LogWarning("cardholderTitle invalid");
                        return bad;
                    }

                    if (string.IsNullOrWhiteSpace(ch.CardholderForename) || ch.CardholderForename.Length > 100)
                    {
                        var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                        await WriteJsonAsync(bad, new ErrorResponse { Code = "CardholderForenameInvalid", Message = "cardholderForename is required and must be <= 100 characters." });
                        logger.LogWarning("cardholderForename invalid");
                        return bad;
                    }

                    if (string.IsNullOrWhiteSpace(ch.CardholderSurname) || ch.CardholderSurname.Length > 100)
                    {
                        var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                        await WriteJsonAsync(bad, new ErrorResponse { Code = "CardholderSurnameInvalid", Message = "cardholderSurname is required and must be <= 100 characters." });
                        logger.LogWarning("cardholderSurname invalid");
                        return bad;
                    }

                    if (string.IsNullOrWhiteSpace(ch.CardholderPhotoName) || ch.CardholderPhotoName.Length > 100)
                    {
                        var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                        await WriteJsonAsync(bad, new ErrorResponse { Code = "PhotoNameInvalid", Message = "cardholderPhotoName is required and must be <= 100 characters." });
                        logger.LogWarning("cardholderPhotoName invalid");
                        return bad;
                    }
                }

                // travelcardNumber length check
                if (string.IsNullOrWhiteSpace(request.TravelcardNumber) || request.TravelcardNumber.Length < 11 || request.TravelcardNumber.Length > 22)
                {
                    var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    await WriteJsonAsync(bad, new ErrorResponse { Code = "TravelcardNumberInvalid", Message = "travelcardNumber is required and must be between 11 and 22 characters." });
                    logger.LogWarning("travelcardNumber invalid");
                    return bad;
                }

                if (string.IsNullOrWhiteSpace(request.TravelcardTransactionReference) || request.TravelcardTransactionReference.Length != 15)
                {
                    var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    await WriteJsonAsync(bad, new ErrorResponse { Code = "TransactionReferenceInvalid", Message = "travelcardTransactionReference is required and must be exactly 15 characters." });
                    logger.LogWarning("transaction reference invalid");
                    return bad;
                }

                // All validations passed. Prepare to persist
                var travelcardGuid = Guid.NewGuid();
                var token = GenerateToken(6);

                try
                {
                    var dbId = await _dbHelper.InsertTravelcardAsync(request, travelcardGuid, token, logger);
                    var created = req.CreateResponse(System.Net.HttpStatusCode.Created);
                    var response = new TravelcardResponse { TravelcardId = travelcardGuid.ToString(), Token = token };
                    await WriteJsonAsync(created, response);
                    logger.LogInformation("Exit CreateTravelcard successfully created db id {Id}", dbId);
                    return created;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Database error while inserting travelcard");
                    var err = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                    await WriteJsonAsync(err, new ErrorResponse { Code = "DatabaseError", Message = "An error occurred while saving the travelcard." });
                    return err;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in CreateTravelcard");
                var resp = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await WriteJsonAsync(resp, new ErrorResponse { Code = "UnhandledError", Message = "An unexpected error occurred." });
                return resp;
            }
        }

        private static async Task WriteJsonAsync(HttpResponseData response, object obj)
        {
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(obj));
        }

        private static string GenerateToken(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var rng = new Random();
            return new string(Enumerable.Range(0, length).Select(_ => chars[rng.Next(chars.Length)]).ToArray());
        }

        private static (bool IsValid, string ErrorMessage) ValidateRequest(TravelcardRequest request)
        {
            if (request.TravelcardType == null)
            {
                return (false, "travelcardType is required.");
            }

            if (!request.TravelcardValidFrom.HasValue) return (false, "travelcardValidFrom is required.");
            if (!request.TravelcardValidTo.HasValue) return (false, "travelcardValidTo is required.");
            if (!request.TravelcardRequestedDate.HasValue) return (false, "travelcardRequestedDate is required.");

            if (!string.IsNullOrEmpty(request.TravelcardName) && request.TravelcardName.Length > 255)
            {
                return (false, "travelcardName must be <= 255 characters.");
            }

            return (true, string.Empty);
        }
    }
}
