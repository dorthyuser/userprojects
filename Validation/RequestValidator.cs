using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Http;
using travelcard_function_app.Helpers;

namespace travelcard_function_app.Validation;

public class RequestValidator
{
    public async Task<ValidationResult> ValidateAsync(HttpRequestData req)
    {
        if (!req.Headers.TryGetValues("client_id", out var clientIds) || string.IsNullOrWhiteSpace(clientIds.FirstOrDefault()) || clientIds.First().Length < 1 || clientIds.First().Length > 128 || !Regex.IsMatch(clientIds.First(), @"^[\w+]+$"))
        {
            return ValidationResult.Fail("Invalid or missing client_id header", "INVALID_CLIENT_ID");
        }

        if (!req.Headers.TryGetValues("Content-Type", out var contentTypes) || !contentTypes.Any(v => v.Contains("application/json", StringComparison.OrdinalIgnoreCase)))
        {
            return ValidationResult.Fail("Content-Type must contain application/json", "INVALID_CONTENT_TYPE");
        }

        if (req.Body == null)
        {
            return ValidationResult.Fail("Request body is required", "MISSING_BODY");
        }

        using var reader = new StreamReader(req.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        req.Body.Position = 0;
        if (string.IsNullOrWhiteSpace(body))
        {
            return ValidationResult.Fail("Request body is required", "MISSING_BODY");
        }

        return ValidationResult.Ok();
    }
}
