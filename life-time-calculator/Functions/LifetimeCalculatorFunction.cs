using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using life_time_calculator.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace life_time_calculator.Functions;

public class LifetimeCalculatorFunction
{
    private readonly ILogger<LifetimeCalculatorFunction> _logger;

    public LifetimeCalculatorFunction(ILogger<LifetimeCalculatorFunction> logger)
    {
        _logger = logger;
    }

    [Function("LifetimeCalculator")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "lifetime-calculator")] HttpRequestData req)
    {
        _logger.LogInformation("Entering LifetimeCalculator function.");
        try
        {
            string? dateOfBirthValue = req.Query["dateOfBirth"];
            if (string.IsNullOrWhiteSpace(dateOfBirthValue))
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                if (!string.IsNullOrWhiteSpace(requestBody))
                {
                    LifetimeCalculatorRequest? body = JsonSerializer.Deserialize<LifetimeCalculatorRequest>(requestBody, JsonOptions.Default);
                    dateOfBirthValue = body?.DateOfBirth;
                }
            }

            if (string.IsNullOrWhiteSpace(dateOfBirthValue))
            {
                return await ErrorResponse.CreateAsync(req, HttpStatusCode.BadRequest, "dateOfBirth is required.", "VALIDATION_ERROR");
            }

            if (!DateTimeOffset.TryParse(dateOfBirthValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset dateOfBirth))
            {
                return await ErrorResponse.CreateAsync(req, HttpStatusCode.BadRequest, "dateOfBirth must be a valid ISO 8601 date-time.", "VALIDATION_ERROR");
            }

            DateTimeOffset currentDate = DateTimeOffset.UtcNow;
            if (dateOfBirth > currentDate)
            {
                return await ErrorResponse.CreateAsync(req, HttpStatusCode.BadRequest, "dateOfBirth cannot be in the future.", "VALIDATION_ERROR");
            }

            LifetimeCalculatorResponse responseModel = LifetimeCalculatorHelper.Calculate(dateOfBirth, currentDate);
            HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(responseModel, JsonOptions.Default);
            _logger.LogInformation("Exiting LifetimeCalculator function successfully.");
            return response;
        }
        catch (Exception)
        {
            _logger.LogError("Error in LifetimeCalculator function.");
            return await ErrorResponse.CreateAsync(req, HttpStatusCode.InternalServerError, "An unexpected error occurred.", "INTERNAL_ERROR");
        }
    }
}
