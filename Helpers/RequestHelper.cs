using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Http;

namespace TravelcardFunctionApp.Helpers;

public static class RequestHelper
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public static async Task<T?> ReadJsonAsync<T>(HttpRequestData req)
    {
        using var reader = new StreamReader(req.Body);
        var json = await reader.ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }
        return JsonSerializer.Deserialize<T>(json, Options);
    }
}