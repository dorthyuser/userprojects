using System.Net.Http;

namespace zohotesting.Services;

public interface IZohoHttpConnectionConnection
{
    Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativePath, HttpContent? content, Dictionary<string, string>? headers, CancellationToken cancellationToken);
    Task<HttpResponseMessage> SendToTokenEndpointAsync(CancellationToken cancellationToken);
}