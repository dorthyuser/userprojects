using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using travelcardlambdachsarp549.Helpers;
using travelcardlambdachsarp549.Models;

namespace travelcardlambdachsarp549.Functions;

public class CreateTravelcardFunction
{
    private readonly DbHelper _dbHelper;
    private readonly ILogger<CreateTravelcardFunction> _logger;

    public CreateTravelcardFunction(DbHelper dbHelper, ILogger<CreateTravelcardFunction> logger)
    {
        _dbHelper = dbHelper;
        _logger = logger;
    }

    [Function("CreateTravelcardFunction")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "travelcards")] HttpRequestData req)
    {
        _logger.LogInformation("Enter CreateTravelcardFunction");
        try
        {
            if (!req.Headers.TryGetValues("client_id", out var clientIds))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Validation failed", "client_id header is required");
            }

            var clientId = clientIds.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(clientId) || clientId.Length > 128)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Validation failed", "client_id must be 1 to 128 characters");
            }

            if (!req.Headers.TryGetValues("Content-Type", out var contentTypes) || !contentTypes.Any(v => v.Contains("application/json", StringComparison.OrdinalIgnoreCase)))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Validation failed", "Content-Type application/json is required");
            }

            var body = await new StreamReader(req.Body, Encoding.UTF8).ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(body))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Validation failed", "Request body is required");
            }

            CreateTravelcardRequest? request;
            try
            {
                request = JsonSerializer.Deserialize<CreateTravelcardRequest>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize request");
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid JSON", ex.Message);
            }

            if (request is null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Validation failed", "Request body is invalid");
            }

            var validationError = TravelcardValidator.Validate(request);
            if (!string.IsNullOrWhiteSpace(validationError))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Validation failed", validationError);
            }

            var result = await _dbHelper.CreateTravelcardAsync(request);
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);
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
        await response.WriteAsJsonAsync(new ErrorResponse(error, details));
        return response;
    }
}