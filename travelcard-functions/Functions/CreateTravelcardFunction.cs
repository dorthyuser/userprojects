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

public sealed class CreateTravelcardFunction
{
    private readonly ILogger<CreateTravelcardFunction> _logger;
    private readonly TravelcardService _service;

    public CreateTravelcardFunction(ILogger<CreateTravelcardFunction> logger, TravelcardService service)
    {
        _logger = logger;
        _service = service;
    }

    [Function("CreateTravelcard")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "travelcard")] HttpRequestData req)
    {
        _logger.LogInformation("ENTRY CreateTravelcard");
        try
        {
            // Validate client_id header
            if (!req.Headers.TryGetValues("client_id", out var clientIds))
            {
                _logger.LogError("Validation failed: field={Field}, reason={Reason}", "client_id", "header missing");
                return await ErrorResponse(req, HttpStatusCode.BadRequest, "Validation Error");
            }

            var clientId = string.Join(",", clientIds);
            if (string.IsNullOrWhiteSpace(clientId) || clientId.Length > 128 ||
                !System.Text.RegularExpressions.Regex.IsMatch(clientId, "^[\\w+]+$"))
            {
                _logger.LogError("Validation failed: field={Field}, reason={Reason}", "client_id", "invalid format or length");
                return await ErrorResponse(req, HttpStatusCode.BadRequest, "Validation Error");
            }

            // Validate Content-Type
            if (!req.Headers.TryGetValues("Content-Type", out var contentTypes) ||
                !string.Join(",", contentTypes).Contains("application/json", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("Validation failed: field={Field}, reason={Reason}", "Content-Type", "must be application/json");
                return await ErrorResponse(req, HttpStatusCode.BadRequest, "Validation Error");
            }

            // Read body
            string body;
            using (var reader = new StreamReader(req.Body))
            {
                body = await reader.ReadToEndAsync();
            }

            if (string.IsNullOrWhiteSpace(body))
            {
                _logger.LogError("Validation failed: field={Field}, reason={Reason}", "body", "required");
                return await ErrorResponse(req, HttpStatusCode.BadRequest, "Validation Error");
            }

            // Deserialize request
            CreateTravelcardRequest? request;
            try
            {
                request = JsonSerializer.Deserialize<CreateTravelcardRequest>(body, JsonOptions.Default);
            }
            catch (JsonException ex)
            {
                _logger.LogError("Parsing error: {Message}", ex.Message);
                return await ErrorResponse(req, HttpStatusCode.BadRequest, "Parsing Error");
            }

            if (request is null)
            {
                _logger.LogError("Validation failed: field={Field}, reason={Reason}", "body", "null after deserialization");
                return await ErrorResponse(req, HttpStatusCode.BadRequest, "Validation Error");
            }

            // Business validation
            var validation = TravelcardValidator.Validate(request);
            if (!validation.IsValid)
            {
                _logger.LogError("Validation failed: {Message}", validation.ErrorMessage);
                return await ErrorResponse(req, HttpStatusCode.BadRequest, "Validation Error");
            }

            // Process
            var result = await _service.CreateAsync(request);

            // ✅ CORRECT: WriteStringAsync + JsonSerializer — NOT WriteAsJsonAsync(result, options)
            var response = req.CreateResponse(HttpStatusCode.Created);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(JsonSerializer.Serialize(result, JsonOptions.Default));

            _logger.LogInformation("EXIT CreateTravelcard — success");
            return response;
        }
        catch (Exception ex)
        {
            // Log full stack trace — return only generic message
            _logger.LogError(ex, "Unexpected error in CreateTravelcard");
            return await ErrorResponse(req, HttpStatusCode.InternalServerError, "Internal Error");
        }
    }

    private static async Task<HttpResponseData> ErrorResponse(
        HttpRequestData req, HttpStatusCode statusCode, string message)
    {
        var response = req.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await response.WriteStringAsync(
            JsonSerializer.Serialize(new ErrorResponse { Error = message }, JsonOptions.Default));
        return response;
    }
}
