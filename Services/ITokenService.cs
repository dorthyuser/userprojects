using System.Threading.Tasks;

namespace ZohoOauthKvTest.Services
{
    public interface ITokenService
    {
        Task<string> GetAccessTokenAsync();
    }
}
