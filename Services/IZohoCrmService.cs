using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ZohoProject3.Models;

namespace ZohoProject3.Services
{
    public interface IZohoCrmService
    {
        Task<IEnumerable<UserDto>> GetUsersAsync(CancellationToken cancellationToken);
        Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken);
        Task<UserDto> UpdateUserAsync(string id, UpdateUserRequest request, CancellationToken cancellationToken);
    }
}
