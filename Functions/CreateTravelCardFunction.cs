using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using TravelCardFunctionApp.Helpers;
using TravelCardFunctionApp.Models;
using TravelCardFunctionApp.Services;

namespace TravelCardFunctionApp.Functions;

public class CreateTravelCardFunction
{
    private readonly ILogger<CreateTravelCardFunction> _logger;
    private readonly TravelCardService _service;

    public CreateTravelCardFunction(ILogger<CreateTravelCardFunction> logger, TravelCardService service)
    {
        _logger = logger;
        _service = service;
    }

    [Function("CreateTravelCard")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "travelcard")] HttpRequestData req)
    {
        _logger.LogInformation("Entering CreateTravelCard function.");
        try
        {
            if (!req.Headers.TryGetValues("client_id", out var clientIds))
            {
                return await ErrorResponse.CreateAsync(req, HttpStatusCode.BadRequest, "client_id header is required.");
            }

            var clientId = string.Join(",", clientIds).Trim();
            if (string.IsNullOrWhiteSpace(clientId) || clientId.Length > 128)
            {
                return await ErrorResponse.CreateAsync(req, HttpStatusCode.BadRequest, "client_id must be between 1 and 128 characters.");
            }

            if (!req.Headers.TryGetValues("Content-Type", out var contentTypes) || !string.Equals(string.Join(",", contentTypes), "application/json", StringComparison.OrdinalIgnoreCase))
            {
                return await ErrorResponse.CreateAsync(req, HttpStatusCode.UnsupportedMediaType, "Content-Type must be application/json.");
            }

            string? correlationId = null;
            if (req.Headers.TryGetValues("X-Correlation-Cust-Id", out var correlationValues))
            {
                correlationId = string.Join(",", correlationValues).Trim();
                if (correlationId.Length > 100)
                {
                    return await ErrorResponse.CreateAsync(req, HttpStatusCode.BadRequest, "X-Correlation-Cust-Id must be 100 characters or fewer.");
                }
            }

            var body = await new StreamReader(req.Body, Encoding.UTF8).ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(body))
            {
                return await ErrorResponse.CreateAsync(req, HttpStatusCode.BadRequest, "Request body is required.");
            }

            var request = JsonHelper.Deserialize<CreateTravelCardRequest>(body);
            if (request is null)
            {
                return await ErrorResponse.CreateAsync(req, HttpStatusCode.BadRequest, "Invalid JSON payload.");
            }

            var validation = TravelCardValidator.Validate(request);
            if (!validation.IsValid)
            {
                return await ErrorResponse.CreateAsync(req, HttpStatusCode.BadRequest, validation.ErrorMessage);
            }

            var result = await _service.CreateAsync(request, clientId, correlationId);
            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(result);
            _logger.LogInformation("Exiting CreateTravelCard function successfully.");
            return response;
        }
        catch (Exception)
        {
            _logger.LogError("Unhandled error in CreateTravelCard function.");
            return await ErrorResponse.CreateAsync(req, HttpStatusCode.InternalServerError, "An unexpected error occurred.");
        }
    }
}
