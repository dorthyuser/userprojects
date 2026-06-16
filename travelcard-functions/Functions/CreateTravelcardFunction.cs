using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
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
            // Validate client_id header (accept both client_id and client-id)
            string? clientId = null;

            if (!TryGetHeaderValue(req, "client_id", out var clientValues) && !TryGetHeaderValue(req, "client-id", out clientValues))
            {
                _logger.LogError("Validation failed: field={Field}, reason={Reason}", "client_id", "header missing");
                return await ErrorResponse(req, HttpStatusCode.BadRequest, "Validation Error: client_id header missing");
            }

            clientId = clientValues?.FirstOrDefault()?.Trim();

            // Validate content and allowed characters. Allow letters, digits, underscore, hyphen, plus and dot.
            if (string.IsNullOrWhiteSpace(clientId) || clientId.Length < 1 || clientId.Length > 128 ||
                !Regex.IsMatch(clientId, @"^[A-Za-z0-9_.+\-]+$"))
            {
                _logger.LogError("Validation failed: field={Field}, reason={Reason}", "client_id", "invalid format or length");
                return await ErrorResponse(req, HttpStatusCode.BadRequest, "Validation Error: client_id invalid format or length");
            }

            // Validate Content-Type
            if (!req.Headers.TryGetValues("Content-Type", out var contentTypes) ||
                !string.Join(",", contentTypes).Contains("application/json", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("Validation failed: field={Field}, reason={Reason}", "Content-Type", "must be application/json");
                return await ErrorResponse(req, HttpStatusCode.BadRequest, "Validation Error: Content-Type must be application/json");
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
                return await ErrorResponse(req, HttpStatusCode.BadRequest, "Validation Error: body required");
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
                return await ErrorResponse(req, HttpStatusCode.BadRequest, "Validation Error: body null");
            }

            // Business validation
            var validation = TravelcardValidator.Validate(request);
            if (!validation.IsValid)
            {
                _logger.LogError("Validation failed: {Message}", validation.ErrorMessage);
                return await ErrorResponse(req, HttpStatusCode.BadRequest, validation.ErrorMessage);
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
        catch (ArgumentException aex)
        {
            // Return user-friendly validation errors (including DB constraint-derived messages)
            _logger.LogError(aex, "Validation error in CreateTravelcard: {Message}", aex.Message);
            return await ErrorResponse(req, HttpStatusCode.BadRequest, aex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in CreateTravelcard: {Message} | StackTrace: {StackTrace}", ex.Message, ex.StackTrace);
            return await ErrorResponse(req, HttpStatusCode.InternalServerError, "Internal Error");
        }
    }

    private static bool TryGetHeaderValue(HttpRequestData req, string headerName, out System.Collections.Generic.IEnumerable<string>? values)
    {
        // HttpRequestData.Headers.TryGetValues is case-insensitive, but some clients replace underscores with hyphens.
        if (req.Headers.TryGetValues(headerName, out values)) return true;

        // Try common alternatives
        var alt = headerName.Replace('_', '-');
        if (alt != headerName && req.Headers.TryGetValues(alt, out values)) return true;

        values = null;
        return false;
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
