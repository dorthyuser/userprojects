using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ZohoProject3.Services
{
    public interface IZohoCrmConnection
    {
        Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativePath, string? jsonBody, CancellationToken cancellationToken);
    }
}
