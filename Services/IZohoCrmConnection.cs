using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace zoho_project_csharp.Services
{
    public interface IZohoCrmConnection
    {
        Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativePath, string? body, CancellationToken cancellationToken);
    }
}
