using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using travelcard_function_app.Helpers;
using travelcard_function_app.Models;
using travelcard_function_app.Services;
using travelcard_function_app.Validation;

namespace travelcard_function_app.Functions;

public class CreateTravelcardFunction
{
    private readonly ILogger<CreateTravelcardFunction> _logger;
    private readonly TravelcardService _travelcardService;
    private readonly RequestValidator _requestValidator;

    public CreateTravelcardFunction(ILogger<CreateTravelcardFunction> logger, TravelcardService travelcardService, RequestValidator requestValidator)
    {
        _logger = logger;
        _travelcardService = travelcardService;
        _requestValidator = requestValidator;
    }

    [Function("CreateTravelcardFunction")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "travelcard")] HttpRequestData req)
    {
        _logger.LogInformation("ENTRY CreateTravelcardFunction");
        try
        {
            var validation = await _requestValidator.ValidateAsync(req);
            if (!validation.IsValid)
            {
                _logger.LogError("ERROR Validation failed: {Message}", validation.ErrorMessage);
                return await ResponseHelper.CreateErrorResponseAsync(req, HttpStatusCode.BadRequest, validation.ErrorMessage ?? "Validation failed", validation.ErrorCode ?? "VALIDATION_ERROR");
            }

            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonHelper.Deserialize<CreateTravelcardRequest>(body);
            if (request == null)
            {
                _logger.LogError("ERROR Invalid JSON payload");
                return await ResponseHelper.CreateErrorResponseAsync(req, HttpStatusCode.BadRequest, "Invalid JSON payload", "INVALID_JSON");
            }

            var businessValidation = TravelcardBusinessValidator.Validate(request);
            if (!businessValidation.IsValid)
            {
                _logger.LogError("ERROR Business validation failed: {Message}", businessValidation.ErrorMessage);
                return await ResponseHelper.CreateErrorResponseAsync(req, HttpStatusCode.BadRequest, businessValidation.ErrorMessage ?? "Business validation failed", businessValidation.ErrorCode ?? "BUSINESS_VALIDATION_ERROR");
            }

            var result = await _travelcardService.CreateAsync(request);
            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(result);
            _logger.LogInformation("EXIT CreateTravelcardFunction");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ERROR Unhandled exception in CreateTravelcardFunction");
            return await ResponseHelper.CreateErrorResponseAsync(req, HttpStatusCode.InternalServerError, "An unexpected error occurred", "INTERNAL_ERROR");
        }
    }
}
