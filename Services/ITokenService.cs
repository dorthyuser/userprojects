using System.Threading.Tasks;

namespace ZohoCrmOauthFinal1.Services
{
    public interface ITokenService
    {
        Task<string> GetAccessTokenAsync();
    }
}