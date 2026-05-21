using synctesting1109.Models;

namespace synctesting1109.Services;

public interface IZohoHttpConnectionConnection
{
    Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativePath, string? jsonBody, string? correlationId, CancellationToken cancellationToken = default);
}