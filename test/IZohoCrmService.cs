using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ZohoProject2.Models;

namespace ZohoProject2.Services
{
    public interface IZohoCrmService
    {
        Task<object> GetUsersAsync(CancellationToken cancellationToken);
        Task<object> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken);
        Task<object> UpdateUserAsync(string id, UpdateUserRequest request, CancellationToken cancellationToken);
    }
}
