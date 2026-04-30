using System.Net;
using System.Text.Json;
using azurecsharpfunction534.Models;
using azurecsharpfunction534.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace azurecsharpfunction534.Functions;

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
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "travelcards")] HttpRequestData req)
    {
        try
        {
            var clientId = req.Headers.TryGetValues("client_id", out var clientIdValues) ? clientIdValues.FirstOrDefault() : string.Empty;
            if (string.IsNullOrWhiteSpace(clientId) || clientId.Length > 128)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "validation_error", "client_id is required and must be between 1 and 128 characters.");
            }

            var body = await new StreamReader(req.Body).ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(body))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "validation_error", "Request body is required.");
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var request = JsonSerializer.Deserialize<CreateTravelcardRequest>(body, options);
            if (request is null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "validation_error", "Invalid JSON payload.");
            }

            var validationError = request.Validate();
            if (!string.IsNullOrEmpty(validationError))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "validation_error", validationError);
            }

            var result = await _dbHelper.CreateTravelcardAsync(request);
            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(result);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating travelcard");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "server_error", ex.Message);
        }
    }

    private static async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, HttpStatusCode statusCode, string error, string details)
    {
        var response = req.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(new ErrorResponse { Error = error, Details = details });
        return response;
    }
}