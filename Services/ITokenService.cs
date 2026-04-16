using System.Threading.Tasks;

namespace hello_http_test.Services
{
    public interface ITokenService
    {
        Task<string> GetAccessTokenAsync();
    }
}
