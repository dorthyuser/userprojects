using System.Threading.Tasks;

namespace ZohoCrmOauthFinal.Services
{
    public interface ITokenService
    {
        Task<string> GetAccessTokenAsync();
    }
}
