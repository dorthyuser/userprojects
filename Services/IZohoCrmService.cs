using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using zoho_project_csharp.Models;

namespace zoho_project_csharp.Services
{
    public interface IZohoCrmService
    {
        Task<(int StatusCode, string Content)> GetUsersAsync(CancellationToken cancellationToken);
        Task<(int StatusCode, string Content)> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken);
        Task<(int StatusCode, string Content)> UpdateUserAsync(string id, UpdateUserRequest request, CancellationToken cancellationToken);
    }
}
