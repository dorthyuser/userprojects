using System.Threading.Tasks;
using ZohoCrmOauthFinal.Models;

namespace ZohoCrmOauthFinal.Services
{
    public interface IZohoUserService
    {
        Task<ZohoUsersResponse?> GetUsersAsync();
    }
}
