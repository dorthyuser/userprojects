using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using TravelCardFunctionApp.Helpers;
using TravelCardFunctionApp.Models;

namespace TravelCardFunctionApp.Functions;

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
        _logger.LogInformation("Entering CreateTravelcard function.");
        try
        {
            if (!req.Headers.TryGetValues("client_id", out var clientIds) || string.IsNullOrWhiteSpace(string.Join(",", clientIds)))
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteAsJsonAsync(new ErrorResponse("validation_error", "client_id header is required."));
                return bad;
            }

            if (!req.Headers.TryGetValues("Content-Type", out var contentTypes) || string.IsNullOrWhiteSpace(string.Join(",", contentTypes)) || !string.Join(",", contentTypes).Contains("application/json", StringComparison.OrdinalIgnoreCase))
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteAsJsonAsync(new ErrorResponse("validation_error", "Content-Type must be application/json."));
                return bad;
            }

            var model = await req.ReadFromJsonAsync<CreateTravelcardRequest>();
            if (model is null)
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteAsJsonAsync(new ErrorResponse("validation_error", "Request body is required."));
                return bad;
            }

            var validation = TravelcardValidator.Validate(model);
            if (!validation.IsValid)
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteAsJsonAsync(new ErrorResponse("validation_error", validation.ErrorMessage));
                return bad;
            }

            var correlationId = req.Headers.TryGetValues("X-Correlation-Cust-Id", out var corrValues) ? string.Join(",", corrValues) : string.Empty;
            var result = await _dbHelper.CreateTravelcardAsync(model, correlationId);

            var ok = req.CreateResponse(HttpStatusCode.OK);
            await ok.WriteAsJsonAsync(result);
            _logger.LogInformation("Exiting CreateTravelcard function successfully.");
            return ok;
        }
        catch (Exception)
        {
            _logger.LogError("CreateTravelcard function failed.");
            var error = req.CreateResponse(HttpStatusCode.InternalServerError);
            await error.WriteAsJsonAsync(new ErrorResponse("internal_server_error", "An unexpected error occurred."));
            return error;
        }
    }
}
