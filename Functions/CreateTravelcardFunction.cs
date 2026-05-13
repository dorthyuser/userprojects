using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using TravelcardFunctionApp.Helpers;
using TravelcardFunctionApp.Models;
using TravelcardFunctionApp.Services;

namespace TravelcardFunctionApp.Functions;

public class CreateTravelcardFunction
{
    private readonly TravelcardDbHelper _dbHelper;
    private readonly ILogger<CreateTravelcardFunction> _logger;

    public CreateTravelcardFunction(TravelcardDbHelper dbHelper, ILogger<CreateTravelcardFunction> logger)
    {
        _dbHelper = dbHelper;
        _logger = logger;
    }

    [Function("CreateTravelcard")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "travelcards")] HttpRequestData req)
    {
        _logger.LogInformation("Entering CreateTravelcardFunction");
        try
        {
            if (!req.Headers.TryGetValues("client_id", out var clientIds))
            {
                return await ResponseHelper.CreateErrorResponse(req, HttpStatusCode.BadRequest, "Missing required header", "client_id header is required.");
            }

            var clientId = string.Join(",", clientIds);
            if (string.IsNullOrWhiteSpace(clientId) || clientId.Length > 128)
            {
                return await ResponseHelper.CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid required header", "client_id must be between 1 and 128 characters.");
            }

            var correlationId = req.Headers.TryGetValues("X-Correlation-Cust-Id", out var corrValues) ? string.Join(",", corrValues) : string.Empty;
            if (!string.IsNullOrWhiteSpace(correlationId) && correlationId.Length > 100)
            {
                return await ResponseHelper.CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid header", "X-Correlation-Cust-Id must be 100 characters or fewer.");
            }

            var body = await RequestHelper.ReadJsonAsync<CreateTravelcardRequest>(req);
            if (body is null)
            {
                return await ResponseHelper.CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request body", "Request body is required and must be valid JSON.");
            }

            var validationError = TravelcardValidator.Validate(body);
            if (!string.IsNullOrWhiteSpace(validationError))
            {
                return await ResponseHelper.CreateErrorResponse(req, HttpStatusCode.BadRequest, "Validation failed", validationError);
            }

            var result = await _dbHelper.CreateTravelcardAsync(body);
            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(new CreateTravelcardResponse
            {
                TravelcardId = result.TravelcardId,
                Token = result.Token
            });

            _logger.LogInformation("Exiting CreateTravelcardFunction successfully");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CreateTravelcardFunction");
            return await ResponseHelper.CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Internal server error", ex.Message);
        }
    }
}