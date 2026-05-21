using System.Net.Http;

namespace synctesting1050.Services;

public interface IZohoHttpConnectionConnection
{
    Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativePath, string? queryString, string? bodyJson, string? ifModifiedSince, CancellationToken cancellationToken = default);
}