using System;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using LifeTimeCalculator.Models;
using LifeTimeCalculator.Helpers;

namespace LifeTimeCalculator.Functions;

public class LifeTimeCalculatorFunction
{
    private readonly ILogger<LifeTimeCalculatorFunction> _logger;

    public LifeTimeCalculatorFunction(ILogger<LifeTimeCalculatorFunction> logger)
    {
        _logger = logger;
    }

    [Function("LifeTimeCalculatorFunction")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "lifetime-calculator")] HttpRequestData req)
    {
        _logger.LogInformation("Entry: LifeTimeCalculatorFunction invoked.");

        try
        {
            string? dobValue = GetQueryParameter(req.Url.Query, "dateOfBirth");
            if (string.IsNullOrWhiteSpace(dobValue))
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteAsJsonAsync(new ErrorResponse("Validation failed", "Query parameter 'dateOfBirth' is required."));
                _logger.LogWarning("Exit: validation failed for LifeTimeCalculatorFunction.");
                return badRequest;
            }

            if (!DateTimeOffset.TryParse(dobValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateOfBirth))
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteAsJsonAsync(new ErrorResponse("Validation failed", "Query parameter 'dateOfBirth' must be a valid ISO 8601 date-time value."));
                _logger.LogWarning("Exit: invalid dateOfBirth format.");
                return badRequest;
            }

            var currentDate = DateTimeOffset.UtcNow;
            if (dateOfBirth > currentDate)
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteAsJsonAsync(new ErrorResponse("Validation failed", "Query parameter 'dateOfBirth' cannot be in the future."));
                _logger.LogWarning("Exit: future dateOfBirth rejected.");
                return badRequest;
            }

            var result = LifeTimeCalculatorHelper.Calculate(dateOfBirth, currentDate);
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);
            _logger.LogInformation("Exit: LifeTimeCalculatorFunction completed successfully.");
            return response;
        }
        catch (Exception)
        {
            _logger.LogError("Error: LifeTimeCalculatorFunction failed.");
            var error = req.CreateResponse(HttpStatusCode.InternalServerError);
            await error.WriteAsJsonAsync(new ErrorResponse("Internal server error", "An unexpected error occurred while calculating lifetime statistics."));
            return error;
        }
    }

    private static string? GetQueryParameter(string queryString, string key)
    {
        if (string.IsNullOrWhiteSpace(queryString))
        {
            return null;
        }

        var query = queryString.StartsWith("?", StringComparison.Ordinal) ? queryString[1..] : queryString;
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2 && string.Equals(Uri.UnescapeDataString(parts[0]), key, StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(parts[1]);
            }
        }

        return null;
    }
}
