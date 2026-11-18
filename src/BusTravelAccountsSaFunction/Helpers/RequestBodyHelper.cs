using Microsoft.AspNetCore.Http;

namespace BusTravelAccountsSaFunction;

public static class RequestBodyHelper
{
    public static bool HasBody(HttpRequest request) =>
        (request.ContentLength.HasValue && request.ContentLength.Value > 0) || request.Body.CanSeek && request.Body.Length > 0;
}