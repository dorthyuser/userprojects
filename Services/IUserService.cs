using System.Threading.Tasks;

namespace ZohoCrmOauthFinal1.Services
{
    public interface IUserService
    {
        Task<string> GetUsersAsync();
        Task<string> CreateUserAsync(object payload);
        Task<string> UpdateUserAsync(string id, object payload);
    }
}