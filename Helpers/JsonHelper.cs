using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Http;

namespace azurefunctionaeproject.Helpers;

public static class JsonHelper
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static async Task<T> ReadFromJsonAsync<T>(HttpRequestData req)
    {
        using var reader = new StreamReader(req.Body);
        var json = await reader.ReadToEndAsync();
        var model = JsonSerializer.Deserialize<T>(json, Options);
        if (model is null)
        {
            throw new ValidationException("MISSING_REQUIRED_FIELD", "Request body is required.");
        }
        return model;
    }

    public static async Task WriteJsonAsync(HttpResponseData response, object payload, HttpStatusCode statusCode)
    {
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(payload, Options));
    }
}
