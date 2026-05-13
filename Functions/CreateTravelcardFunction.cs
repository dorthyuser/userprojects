using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using azurecsharppost1112.Helpers;
using azurecsharppost1112.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace azurecsharppost1112.Functions;

public class CreateTravelcardFunction
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(null, allowIntegerValues: false) }
    };

    private readonly DbHelper _dbHelper;
    private readonly ILogger<CreateTravelcardFunction> _logger;

    public CreateTravelcardFunction(DbHelper dbHelper, ILogger<CreateTravelcardFunction> logger)
    {
        _dbHelper = dbHelper;
        _logger = logger;
    }

    [Function("CreateTravelcard")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "travelcard")] HttpRequestData req)
    {
        _logger.LogInformation("Entering CreateTravelcard function");
        try
        {
            if (!req.Headers.TryGetValues("client_id", out var clientIds) || string.IsNullOrWhiteSpace(clientIds.FirstOrDefault()))
            {
                return await CreateError(req, HttpStatusCode.BadRequest, "Missing required header", "client_id is required.");
            }

            if (req.Headers.TryGetValues("X-Correlation-Cust-Id", out var correlationIds))
            {
                var correlationId = correlationIds.FirstOrDefault();
                if (!string.IsNullOrEmpty(correlationId) && correlationId.Length > 100)
                {
                    return await CreateError(req, HttpStatusCode.BadRequest, "Invalid header", "X-Correlation-Cust-Id must be 100 characters or fewer.");
                }
            }

            var body = await new StreamReader(req.Body).ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(body))
            {
                return await CreateError(req, HttpStatusCode.BadRequest, "Invalid request", "Request body is required.");
            }

            var model = JsonSerializer.Deserialize<CreateTravelcardRequest>(body, JsonOptions);
            if (model is null)
            {
                return await CreateError(req, HttpStatusCode.BadRequest, "Invalid request", "Unable to parse request body.");
            }

            var validationError = TravelcardValidator.Validate(model);
            if (!string.IsNullOrEmpty(validationError))
            {
                return await CreateError(req, HttpStatusCode.BadRequest, "Validation failed", validationError);
            }

            var result = await _dbHelper.CreateTravelcardAsync(model);
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new CreateTravelcardResponse
            {
                travelcardId = result.TravelcardId,
                token = result.Token
            });

            _logger.LogInformation("Exiting CreateTravelcard function successfully");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CreateTravelcard function");
            return await CreateError(req, HttpStatusCode.InternalServerError, "Internal server error", ex.Message);
        }
    }

    private static async Task<HttpResponseData> CreateError(HttpRequestData req, HttpStatusCode statusCode, string error, string details)
    {
        var response = req.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(new ErrorResponse
        {
            error = error,
            details = details
        });
        return response;
    }
}
