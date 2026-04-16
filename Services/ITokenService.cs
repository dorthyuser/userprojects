using System.Threading.Tasks;

namespace TcTestingZoho.Services
{
    public interface ITokenService
    {
        Task<string> GetAccessTokenAsync();
        string GetClientId();
    }
}
