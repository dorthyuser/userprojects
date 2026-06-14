using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using travelcard_functions.Helpers;
using travelcard_functions.Models;
using travelcard_functions.Validation;

namespace travelcard_functions.Functions;

public class CreateTravelcardFunction
{
    private readonly ILogger<CreateTravelcardFunction> _logger;
    private readonly TravelcardService _service;

    public CreateTravelcardFunction(ILogger<CreateTravelcardFunction> logger, TravelcardService service)
    {
        _logger = logger;
        _service = service;
    }

    [Function("CreateTravelcard")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "travelcard")] HttpRequestData req)
    {
        _logger.LogInformation("ENTRY CreateTravelcard");
        try
        {
            if (!req.Headers.TryGetValues("client_id", out var clientIds))
            {
                return await ErrorResponse(req, HttpStatusCode.BadRequest, "client_id header is required.");
            }

            var clientId = string.Join(",", clientIds);
            if (string.IsNullOrWhiteSpace(clientId) || clientId.Length > 128 || !System.Text.RegularExpressions.Regex.IsMatch(clientId, "^[\\w+]+$"))
            {
                return await ErrorResponse(req, HttpStatusCode.BadRequest, "client_id header is invalid.");
            }

            if (!req.Headers.TryGetValues("Content-Type", out var contentTypes) || !string.Join(",", contentTypes).Contains("application/json", StringComparison.OrdinalIgnoreCase))
            {
                return await ErrorResponse(req, HttpStatusCode.BadRequest, "Content-Type must contain application/json.");
            }

            string body;
            using (var reader = new StreamReader(req.Body))
            {
                body = await reader.ReadToEndAsync();
            }

            if (string.IsNullOrWhiteSpace(body))
            {
                return await ErrorResponse(req, HttpStatusCode.BadRequest, "Request body is required.");
            }

            CreateTravelcardRequest? request;
            try
            {
                request = JsonSerializer.Deserialize<CreateTravelcardRequest>(body, JsonOptions.Default);
            }
            catch
            {
                return await ErrorResponse(req, HttpStatusCode.BadRequest, "Invalid JSON payload.");
            }

            if (request is null)
            {
                return await ErrorResponse(req, HttpStatusCode.BadRequest, "Request body is required.");
            }

            var validation = TravelcardValidator.Validate(request);
            if (!validation.IsValid)
            {
                _logger.LogError("ERROR Validation failed: {Message}", validation.ErrorMessage);
                return await ErrorResponse(req, HttpStatusCode.BadRequest, validation.ErrorMessage);
            }

            var result = await _service.CreateAsync(request);
            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(result);
            _logger.LogInformation("EXIT CreateTravelcard");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ERROR Unexpected error in CreateTravelcard");
            return await ErrorResponse(req, HttpStatusCode.InternalServerError, "An unexpected error occurred.");
        }
    }

    private static async Task<HttpResponseData> ErrorResponse(HttpRequestData req, HttpStatusCode statusCode, string message)
    {
        var response = req.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(new ErrorResponse { Error = message });
        return response;
    }
}
