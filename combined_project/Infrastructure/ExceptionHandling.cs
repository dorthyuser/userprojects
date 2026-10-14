using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Microsoft.AspNetCore.Diagnostics;

namespace combined_project.Infrastructure;

public static class ExceptionHandling
{
    public static APIGatewayProxyResponse Error(int statusCode, string message) => new()
    {
        StatusCode = statusCode,
        Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
        Body = JsonSerializer.Serialize(new { message })
    };
}
