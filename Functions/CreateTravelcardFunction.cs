using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using azuretravelcardfunction309.Helpers;
using azuretravelcardfunction309.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace azuretravelcardfunction309.Functions;

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
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "travelcard")] HttpRequestData req)
    {
        _logger.LogInformation("Enter CreateTravelcardFunction");

        try
        {
            if (!req.Headers.TryGetValues("client_id", out var clientIds))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Missing header", "client_id header is required.");
            }

            var clientId = clientIds.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(clientId) || clientId.Length > 128)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid header", "client_id must be 1 to 128 characters.");
            }

            if (!req.Headers.TryGetValues("Content-Type", out var contentTypes) || !string.Equals(contentTypes.FirstOrDefault(), "application/json", StringComparison.OrdinalIgnoreCase))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid header", "Content-Type must be application/json.");
            }

            string requestBody;
            using (var reader = new StreamReader(req.Body))
            {
                requestBody = await reader.ReadToEndAsync();
            }

            if (string.IsNullOrWhiteSpace(requestBody))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid body", "Request body is required.");
            }

            CreateTravelcardRequest? model;
            try
            {
                model = JsonSerializer.Deserialize<CreateTravelcardRequest>(requestBody, JsonOptionsFactory.Options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JSON deserialization failed");
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid JSON", ex.Message);
            }

            if (model is null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid body", "Unable to parse request body.");
            }

            var validationError = TravelcardValidator.Validate(model);
            if (!string.IsNullOrWhiteSpace(validationError))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Validation failed", validationError);
            }

            var result = await _dbHelper.CreateTravelcardAsync(model);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(result, JsonOptionsFactory.Options));
            _logger.LogInformation("Exit CreateTravelcardFunction successfully");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in CreateTravelcardFunction");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Internal server error", ex.Message);
        }
    }

    private static async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, HttpStatusCode statusCode, string error, string details)
    {
        var response = req.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json");
        var payload = new ErrorResponse(error, details);
        await response.WriteStringAsync(JsonSerializer.Serialize(payload, JsonOptionsFactory.Options));
        return response;
    }
}
