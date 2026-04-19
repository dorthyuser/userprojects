using System.Threading;
using System.Threading.Tasks;
using ZohoProject1.Models;

namespace ZohoProject1.Services
{
    public interface IZohoCrmService
    {
        Task<string> GetUsersAsync(CancellationToken cancellationToken);
        Task<string> CreateUserAsync(User user, CancellationToken cancellationToken);
        Task<string> UpdateUserAsync(string id, User user, CancellationToken cancellationToken);
    }
}
