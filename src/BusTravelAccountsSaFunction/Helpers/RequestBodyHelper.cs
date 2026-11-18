using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace BusTravelAccountsSaFunction;

public static class RequestBodyHelper
{
    public static async Task<T> ReadRequiredJsonAsync<T>(HttpRequest req, CancellationToken cancellationToken)
    {
        if (req.ContentLength == 0)
        {
            throw new EmptyBodyException();
        }

        if (req.Body.CanSeek && req.Body.Length == 0)
        {
            throw new EmptyBodyException();
        }

        var result = await JsonSerializer.DeserializeAsync<T>(req.Body, JsonDefaults.Options, cancellationToken);
        if (result is null)
        {
            throw new EmptyBodyException();
        }

        return result;
    }
}