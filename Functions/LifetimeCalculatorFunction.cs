using System;
using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using life_time_calculator.Helpers;
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
        _logger.LogInformation("Entry: LifetimeCalculator request received.");

        try
        {
            string? dateOfBirthValue = QueryStringHelper.GetQueryParameter(req.Url, "dateOfBirth");
            if (string.IsNullOrWhiteSpace(dateOfBirthValue))
            {
                return await ErrorResponseHelper.CreateAsync(req, HttpStatusCode.BadRequest, "dateOfBirth is required.");
            }

            if (!DateTimeOffset.TryParse(dateOfBirthValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset dateOfBirth))
            {
                return await ErrorResponseHelper.CreateAsync(req, HttpStatusCode.BadRequest, "dateOfBirth must be a valid ISO 8601 date-time value.");
            }

            DateTimeOffset currentDate = DateTimeOffset.UtcNow;
            if (dateOfBirth > currentDate)
            {
                return await ErrorResponseHelper.CreateAsync(req, HttpStatusCode.BadRequest, "dateOfBirth cannot be in the future.");
            }

            LifetimeCalculationResult result = LifetimeCalculator.Calculate(dateOfBirth, currentDate);

            HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(JsonSerializer.Serialize(result, JsonOptionsProvider.Options));

            _logger.LogInformation("Exit: LifetimeCalculator request completed successfully.");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error: LifetimeCalculator request failed.");
            return await ErrorResponseHelper.CreateAsync(req, HttpStatusCode.InternalServerError, "An unexpected error occurred.");
        }
    }
}
